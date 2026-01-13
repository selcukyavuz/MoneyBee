using Microsoft.Extensions.Options;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Common.Services;
using MoneyBee.Transfer.Service.Application.Constants;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Application.Interfaces;
using MoneyBee.Transfer.Service.Application.Mappers;
using MoneyBee.Transfer.Service.Application.Options;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;
using MoneyBee.Transfer.Service.Domain.Interfaces;
using MoneyBee.Transfer.Service.Domain.Validators;
using MoneyBee.Transfer.Service.Helpers;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.ExchangeRateService;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices.FraudDetectionService;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;

namespace MoneyBee.Transfer.Service.Application.Services;

public class TransferService(
    ITransferRepository repository,
    IFraudDetectionService fraudService,
    IEventPublisher eventPublisher,
    ILogger<TransferService> logger,
    IOptions<TransferSettings> transferSettings,
    IOptions<FeeSettings> feeSettings,
    TransferValidationService validationService,
    TransferCodeGenerator codeGenerator) : ITransferService
{
    private readonly TransferSettings _transferSettings = transferSettings.Value;
    private readonly FeeSettings _feeSettings = feeSettings.Value;

    public async Task<Result<CreateTransferResponse>> CreateTransferAsync(CreateTransferRequest request, CancellationToken cancellationToken = default)
    {
        // Check idempotency - returns error if key is missing, existing transfer if found
        var idempotencyCheck = await validationService.CheckIdempotencyAsync(request.IdempotencyKey);
        if (idempotencyCheck.HasValue)
        {
            // Return either the error (if key is missing) or existing transfer (if duplicate request)
            return idempotencyCheck.Value;
        }

        // Validate amount
        if (request.Amount <= 0)
        {
            return Result<CreateTransferResponse>.Failure(TransferErrors.AmountMustBePositive);
        }

        // Validate sender
        var senderResult = await validationService.ValidateCustomerAsync(
            request.SenderNationalId,
            TransferErrors.SenderNotFound,
            TransferErrors.SenderNotActive,
            cancellationToken);
        if (!senderResult.IsSuccess)
        {
            return Result<CreateTransferResponse>.Failure(senderResult.Error!);
        }
        
        var sender = senderResult.Value;

        // Validate receiver
        var receiverResult = await validationService.ValidateCustomerAsync(
            request.ReceiverNationalId,
            TransferErrors.ReceiverNotFound,
            TransferErrors.ReceiverNotActive,
            cancellationToken);
        if (!receiverResult.IsSuccess)
        {
            return Result<CreateTransferResponse>.Failure(receiverResult.Error!);
        }
        
        var receiver = receiverResult.Value;

        // Calculate amount in TRY with exchange rate
        var amountResult = await validationService.CalculateAmountInTRYAsync(request.Amount, request.Currency);
        if (!amountResult.IsSuccess)
        {
            return Result<CreateTransferResponse>.Failure(amountResult.Error!);
        }
        
        var transferAmount = amountResult.Value;

        // Check daily limit with distributed lock
        var dailyLimitResult = await validationService.CheckDailyLimitWithLockAsync(sender.Id, transferAmount.AmountInTRY);
        if (!dailyLimitResult.IsSuccess)
        {
            return Result<CreateTransferResponse>.Failure(dailyLimitResult.Error!);
        }

        // Perform fraud check
        var fraudCheckResult = await fraudService.CheckTransferAsync(
            sender.Id, receiver.Id, transferAmount.AmountInTRY, sender.NationalId);

        if (!fraudCheckResult.IsSuccess)
        {
            logger.LogError("Failed to check fraud for transfer: {Error}", fraudCheckResult.Error);
            return Result<CreateTransferResponse>.Failure(TransferErrors.FraudCheckFailed);
        }

        var fraudCheck = fraudCheckResult.Value!;

        // Reject if HIGH risk
        if (TransferValidator.ShouldRejectTransfer(fraudCheck.RiskLevel))
        {
            await CreateFailedTransferAsync(sender, receiver, request, transferAmount.AmountInTRY, transferAmount.ExchangeRate, fraudCheck.RiskLevel);
            return Result<CreateTransferResponse>.Failure(TransferErrors.HighFraudRisk);
        }

        // Calculate fee
        var transactionFee = FeeCalculator.Calculate(transferAmount.AmountInTRY, _feeSettings.BaseFee, _feeSettings.FeePercentage);

        // Determine approval wait time
        var approvalRequiredUntil = TransferValidator.CalculateApprovalWaitTime(
            transferAmount.AmountInTRY, _transferSettings.HighAmountThresholdTRY, _transferSettings.ApprovalWaitMinutes);

        // Create and save transfer
        var transfer = TransferEntity.Create(
            sender.Id,
            receiver.Id,
            request.Amount,
            request.Currency,
            transferAmount.AmountInTRY,
            transferAmount.ExchangeRate,
            transactionFee,
            await codeGenerator.GenerateUniqueCodeAsync(),
            fraudCheck.RiskLevel,
            request.IdempotencyKey,
            approvalRequiredUntil,
            sender.NationalId,
            receiver.NationalId);

        await repository.CreateAsync(transfer);

        // Publish event
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

    public async Task<Result<TransferDto>> CompleteTransferAsync(string transactionCode, CompleteTransferRequest request)
    {
        var transfer = await repository.GetByTransactionCodeAsync(transactionCode);

        if (transfer is null)
        {
            return Result<TransferDto>.Failure(TransferErrors.TransferNotFound);
        }

        // Use domain service for validation
        var validationResult = TransferValidator.ValidateTransferForCompletion(transfer, request.ReceiverNationalId);
        if (!validationResult.IsSuccess)
        {
            return Result<TransferDto>.Failure(validationResult.Error!);
        }

        // Use aggregate method to complete
        transfer.Complete();

        await repository.UpdateAsync(transfer);

        // Publish integration event directly to RabbitMQ
        await eventPublisher.PublishAsync(new TransferCompletedEvent
        {
            TransferId = transfer.Id,
            TransactionCode = transfer.TransactionCode
        });

        logger.LogInformation("Transfer completed: {TransferId} - Code: {TransactionCode}",
            transfer.Id, transfer.TransactionCode);

        return Result<TransferDto>.Success(TransferMapper.ToDto(transfer));
    }

    public async Task<Result<TransferDto>> CancelTransferAsync(string transactionCode, CancelTransferRequest request)
    {
        var transfer = await repository.GetByTransactionCodeAsync(transactionCode);

        if (transfer is null)
        {
            return Result<TransferDto>.Failure(TransferErrors.TransferNotFound);
        }

        // Use aggregate method to cancel
        transfer.Cancel(request.Reason);

        await repository.UpdateAsync(transfer);

        // Publish integration event directly to RabbitMQ
        await eventPublisher.PublishAsync(new TransferCancelledEvent
        {
            TransferId = transfer.Id,
            Reason = request.Reason ?? string.Empty
        });

        logger.LogInformation("Transfer cancelled: {TransferId} - Reason: {Reason}",
            transfer.Id, request.Reason);

        return Result<TransferDto>.Success(TransferMapper.ToDto(transfer));
    }

    public async Task<Result<TransferDto>> GetTransferByCodeAsync(string transactionCode)
    {
        var transfer = await repository.GetByTransactionCodeAsync(transactionCode);
        
        if (transfer is null)
        {
            return Result<TransferDto>.Failure(TransferErrors.TransferNotFound);
        }
        
        return Result<TransferDto>.Success(TransferMapper.ToDto(transfer));
    }

    public async Task<IEnumerable<TransferDto>> GetCustomerTransfersAsync(Guid customerId)
    {
        var transfers = await repository.GetCustomerTransfersAsync(customerId);
        return transfers.Select(TransferMapper.ToDto);
    }

    public async Task<DailyLimitCheckResponse> CheckDailyLimitAsync(Guid customerId)
    {
        var totalToday = await repository.GetDailyTotalAsync(customerId, DateTime.Today);

        return new DailyLimitCheckResponse
        {
            TotalTransfersToday = totalToday,
            DailyLimit = _transferSettings.DailyLimitTRY
        };
    }

    private async Task CreateFailedTransferAsync(
        CustomerInfo sender,
        CustomerInfo receiver, 
        CreateTransferRequest request,
        decimal amountInTRY,
        decimal? exchangeRate,
        RiskLevel riskLevel)
    {
        logger.LogWarning("Transfer rejected due to HIGH risk: Sender={SenderId}", sender.Id);
        
        var rejectedTransfer = TransferEntity.CreateFailed(
            sender.Id,
            receiver.Id,
            request.Amount,
            request.Currency,
            amountInTRY,
            exchangeRate,
            TransactionCodeGenerator.Generate(),
            riskLevel,
            request.IdempotencyKey,
            sender.NationalId,
            receiver.NationalId);

        await repository.CreateAsync(rejectedTransfer);
    }
}
