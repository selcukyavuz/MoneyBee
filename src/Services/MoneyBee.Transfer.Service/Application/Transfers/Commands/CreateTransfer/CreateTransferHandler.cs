using Microsoft.Extensions.Options;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Common.Abstractions;
using MoneyBee.Transfer.Service.Application.Transfers.Options;
using MoneyBee.Transfer.Service.Application.Transfers.Services;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Domain.Transfers;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Transfers.Transfer;

namespace MoneyBee.Transfer.Service.Application.Transfers.Commands.CreateTransfer;

/// <summary>
/// Handles the creation of money transfers with validation, fraud checking, and event publishing
/// </summary>
public class CreateTransferHandler(
    ITransferRepository repository,
    ICustomerService customerService,
    IExchangeRateService exchangeRateService,
    IFraudDetectionService fraudService,
    IDistributedLockService distributedLock,
    IEventPublisher eventPublisher,
    ITransactionCodeGenerator codeGenerator,
    IOptions<TransferSettings> transferSettings,
    IOptions<FeeSettings> feeSettings,
    ILogger<CreateTransferHandler> logger) : ICommandHandler<CreateTransferRequest, Result<CreateTransferResponse>>
{
    private readonly TransferSettings _transferSettings = transferSettings.Value;
    private readonly FeeSettings _feeSettings = feeSettings.Value;

    public async Task<Result<CreateTransferResponse>> HandleAsync(CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Check idempotency
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Result<CreateTransferResponse>.Validation(TransferErrors.IdempotencyKeyRequired);
        }

        var existingTransfer = await repository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
        if (existingTransfer is not null)
        {
            logger.LogInformation("Idempotent request detected: {IdempotencyKey}", request.IdempotencyKey);
            return Result<CreateTransferResponse>.Success(TransferMapper.ToCreateResponse(existingTransfer));
        }

        // 2. Validate amount
        if (request.Amount <= 0)
        {
            return Result<CreateTransferResponse>.Validation(TransferErrors.AmountMustBePositive);
        }

        // 3. Validate sender
        var senderResult = await customerService.GetCustomerByNationalIdAsync(request.SenderNationalId, cancellationToken);
        if (!senderResult.IsSuccess || senderResult.Value is null)
        {
            return Result<CreateTransferResponse>.Failure(TransferErrors.SenderNotFound);
        }
        
        var sender = senderResult.Value;
        if (sender.Status != CustomerStatus.Active)
        {
            return Result<CreateTransferResponse>.Failure(TransferErrors.SenderNotActive);
        }

        // 4. Validate receiver
        var receiverResult = await customerService.GetCustomerByNationalIdAsync(request.ReceiverNationalId, cancellationToken);
        if (!receiverResult.IsSuccess || receiverResult.Value is null)
        {
            return Result<CreateTransferResponse>.Failure(TransferErrors.ReceiverNotFound);
        }
        
        var receiver = receiverResult.Value;
        if (receiver.Status != CustomerStatus.Active)
        {
            return Result<CreateTransferResponse>.Failure(TransferErrors.ReceiverNotActive);
        }

        // 5. Calculate amount in TRY with exchange rate
        decimal amountInTRY;
        decimal? exchangeRate = null;

        if (request.Currency == Currency.TRY)
        {
            amountInTRY = request.Amount;
        }
        else
        {
            var rateResult = await exchangeRateService.GetExchangeRateAsync(request.Currency, Currency.TRY, cancellationToken);
            if (!rateResult.IsSuccess)
            {
                logger.LogError("Failed to get exchange rate for {Currency}: {Error}", request.Currency, rateResult.Error);
                return Result<CreateTransferResponse>.Failure(TransferErrors.ExchangeRateUnavailable);
            }

            exchangeRate = rateResult.Value!.Rate;
            amountInTRY = request.Amount * exchangeRate.Value;
        }

        // 6. Check daily limit with distributed lock
        try
        {
            await distributedLock.ExecuteWithLockAsync(
                lockKey: $"customer:{sender.Id}:daily-limit",
                expiry: TimeSpan.FromSeconds(_transferSettings.DailyLimitCheckTimeoutSeconds),
                async () =>
                {
                    var dailyTotal = await repository.GetDailyTotalAsync(sender.Id, DateTime.Today);
                    var result = TransferEntity.ValidateDailyLimit(dailyTotal, amountInTRY, _transferSettings.DailyLimitTRY);
                    
                    if (!result.IsSuccess)
                    {
                        throw new InvalidOperationException(result.Error);
                    }
                    
                    logger.LogDebug(
                        "Daily limit check passed for customer {CustomerId}: {DailyTotal} + {Amount} <= {Limit}",
                        sender.Id, dailyTotal, amountInTRY, _transferSettings.DailyLimitTRY);
                    
                    return true;
                });
        }
        catch (InvalidOperationException ex)
        {
            return Result<CreateTransferResponse>.Failure(ex.Message);
        }

        // 7. Perform fraud check
        var fraudCheckResult = await fraudService.CheckTransferAsync(
            sender.Id, 
            receiver.Id, 
            amountInTRY, 
            sender.NationalId,
            cancellationToken);

        if (!fraudCheckResult.IsSuccess)
        {
            logger.LogError("Failed to check fraud for transfer: {Error}", fraudCheckResult.Error);
            return Result<CreateTransferResponse>.Failure(TransferErrors.FraudCheckFailed);
        }

        var fraudCheck = fraudCheckResult.Value!;

        // 8. Reject if HIGH risk - use static helper since we don't have entity yet
        if (fraudCheck.RiskLevel == RiskLevel.High)
        {
            logger.LogWarning("Creating failed transfer entity due to HIGH risk: Sender={SenderId}", sender.Id);

            var rejectedTransfer = TransferEntity.CreateFailed(
                sender.Id,
                receiver.Id,
                request.Amount,
                request.Currency,
                amountInTRY,
                exchangeRate,
                codeGenerator.Generate(),
                fraudCheck.RiskLevel,
                request.IdempotencyKey,
                sender.NationalId,
                receiver.NationalId);

            await repository.CreateAsync(rejectedTransfer);
            return Result<CreateTransferResponse>.Failure(TransferErrors.HighFraudRisk);
        }

        // 9. Calculate fee
        var transactionFee = TransferEntity.CalculateFee(amountInTRY, _feeSettings.BaseFee, _feeSettings.FeePercentage);

        // 10. Determine approval wait time
        var approvalRequiredUntil = TransferEntity.CalculateApprovalWaitTime(
            amountInTRY, _transferSettings.HighAmountThresholdTRY, _transferSettings.ApprovalWaitMinutes);

        // 11. Generate unique transaction code
        var transactionCode = await GenerateUniqueCodeAsync();

        // 12. Create transfer entity
        var transfer = TransferEntity.Create(
            sender.Id,
            receiver.Id,
            request.Amount,
            request.Currency,
            amountInTRY,
            exchangeRate,
            transactionFee,
            transactionCode,
            fraudCheck.RiskLevel,
            request.IdempotencyKey,
            approvalRequiredUntil,
            sender.NationalId,
            receiver.NationalId);

        // 13. Save transfer
        await repository.CreateAsync(transfer);

        // 14. Publish event
        await eventPublisher.PublishAsync(new TransferCreatedEvent
        {
            TransferId = transfer.Id,
            SenderId = transfer.SenderId,
            ReceiverId = transfer.ReceiverId,
            Amount = transfer.Amount,
            Currency = transfer.Currency.ToString()
        });

        logger.LogInformation("Transfer created: {TransferId} - Code: {TransactionCode}, Amount: {Amount} {Currency}",
            transfer.Id, transfer.TransactionCode, transfer.Amount, transfer.Currency);

        return Result<CreateTransferResponse>.Success(TransferMapper.ToCreateResponse(transfer));
    }

    private async Task<string> GenerateUniqueCodeAsync()
    {
        string code;
        bool exists;

        do
        {
            code = codeGenerator.Generate();
            exists = await repository.TransactionCodeExistsAsync(code);
        }
        while (exists);

        return code;
    }
}
