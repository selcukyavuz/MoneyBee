using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Exceptions;
using MoneyBee.Common.Results;
using MoneyBee.Common.Services;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Application.Interfaces;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;
using MoneyBee.Transfer.Service.Domain.Interfaces;
using MoneyBee.Transfer.Service.Domain.Validators;
using MoneyBee.Transfer.Service.Helpers;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices;
using MoneyBee.Transfer.Service.Infrastructure.Messaging;

namespace MoneyBee.Transfer.Service.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRepository _repository;
    private readonly ICustomerService _customerService;
    private readonly IFraudDetectionService _fraudService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDistributedLockService _distributedLock;
    private readonly ILogger<TransferService> _logger;

    private const decimal DAILY_LIMIT_TRY = 10000m;

    public TransferService(
        ITransferRepository repository,
        ICustomerService customerService,
        IFraudDetectionService fraudService,
        IExchangeRateService exchangeRateService,
        IEventPublisher eventPublisher,
        IDistributedLockService distributedLock,
        ILogger<TransferService> logger)
    {
        _repository = repository;
        _customerService = customerService;
        _fraudService = fraudService;
        _exchangeRateService = exchangeRateService;
        _eventPublisher = eventPublisher;
        _distributedLock = distributedLock;
        _logger = logger;
    }

    public async Task<Result<CreateTransferResponse>> CreateTransferAsync(CreateTransferRequest request)
    {
        // Check idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingTransfer = await _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existingTransfer != null)
            {
                _logger.LogInformation("Idempotent request detected: {IdempotencyKey}", request.IdempotencyKey);
                return Result<CreateTransferResponse>.Success(MapToCreateResponse(existingTransfer));
            }
        }

        // Validate amount
        if (request.Amount <= 0)
            return Result<CreateTransferResponse>.Failure("Amount must be greater than zero");

        // Get sender customer
        var senderResult = await _customerService.GetCustomerByNationalIdAsync(request.SenderNationalId);
        if (!senderResult.IsSuccess)
            return Result<CreateTransferResponse>.Failure("Sender customer not found");
        
        var sender = senderResult.Value!;
        if (sender.Status != CustomerStatus.Active)
            return Result<CreateTransferResponse>.Failure("Sender customer is not active");

        // Get receiver customer
        var receiverResult = await _customerService.GetCustomerByNationalIdAsync(request.ReceiverNationalId);
        if (!receiverResult.IsSuccess)
            return Result<CreateTransferResponse>.Failure("Receiver customer not found");
        
        var receiver = receiverResult.Value!;
        if (receiver.Status == CustomerStatus.Blocked)
            return Result<CreateTransferResponse>.Failure("Receiver customer is blocked");

        // Get exchange rate if needed
        decimal amountInTRY;
        decimal? exchangeRate = null;

        if (request.Currency != Currency.TRY)
        {
            try
            {
                var rateResult = await _exchangeRateService.GetExchangeRateAsync(request.Currency, Currency.TRY);
                exchangeRate = rateResult.Rate;
                amountInTRY = request.Amount * exchangeRate.Value;
            }
            catch (ExternalServiceException ex)
            {
                _logger.LogError(ex, "Failed to get exchange rate");
                return Result<CreateTransferResponse>.Failure("Exchange rate service unavailable. Please try again later.");
            }
        }
        else
        {
            amountInTRY = request.Amount;
        }

        // Check daily limit with distributed lock to prevent race conditions
        try
        {
            await _distributedLock.ExecuteWithLockAsync(
                lockKey: $"customer:{sender.Id}:daily-limit",
                expiry: TimeSpan.FromSeconds(10),
                async () =>
                {
                    var dailyTotal = await _repository.GetDailyTotalAsync(sender.Id, DateTime.Today);
                    var result = TransferValidator.ValidateDailyLimit(dailyTotal, amountInTRY, DAILY_LIMIT_TRY);
                    
                    if (!result.IsSuccess)
                        throw new InvalidOperationException(result.Error);
                    
                    _logger.LogDebug(
                        "Daily limit check passed for customer {CustomerId}: {DailyTotal} + {Amount} <= {Limit}",
                        sender.Id, dailyTotal, amountInTRY, DAILY_LIMIT_TRY);
                    
                    return true;
                });
        }
        catch (InvalidOperationException ex)
        {
            return Result<CreateTransferResponse>.Failure(ex.Message);
        }

        // Perform fraud check
        var fraudCheck = await _fraudService.CheckTransferAsync(
            sender.Id, receiver.Id, amountInTRY, sender.NationalId);

        // Reject if HIGH risk using domain service
        if (TransferValidator.ShouldRejectTransfer(fraudCheck.RiskLevel))
        {
            _logger.LogWarning("Transfer rejected due to HIGH risk: Sender={SenderId}", sender.Id);
            
            var rejectedTransfer = TransferEntity.CreateFailed(
                sender.Id,
                receiver.Id,
                request.Amount,
                request.Currency,
                amountInTRY,
                exchangeRate,
                TransactionCodeGenerator.Generate(),
                fraudCheck.RiskLevel,
                request.IdempotencyKey,
                sender.NationalId,
                receiver.NationalId);

            await _repository.CreateAsync(rejectedTransfer);

            return Result<CreateTransferResponse>.Failure("Transfer rejected due to high fraud risk");
        }

        // Calculate fee
        var transactionFee = FeeCalculator.Calculate(amountInTRY);

        // Use domain service to determine approval wait
        var approvalRequiredUntil = TransferValidator.CalculateApprovalWaitTime(amountInTRY);

        // Create transfer using aggregate factory method
        var transfer = TransferEntity.Create(
            sender.Id,
            receiver.Id,
            request.Amount,
            request.Currency,
            amountInTRY,
            exchangeRate,
            transactionFee,
            await GenerateUniqueTransactionCodeAsync(),
            fraudCheck.RiskLevel,
            request.IdempotencyKey,
            approvalRequiredUntil,
            sender.NationalId,
            receiver.NationalId);

        await _repository.CreateAsync(transfer);

        // Publish integration event directly to RabbitMQ
        await _eventPublisher.PublishAsync(new TransferCreatedEvent
        {
            TransferId = transfer.Id,
            SenderId = transfer.SenderId,
            ReceiverId = transfer.ReceiverId,
            Amount = transfer.Amount,
            Currency = transfer.Currency.ToString()
        });

        _logger.LogInformation("Transfer created: {TransferId} - Code: {TransactionCode}, Amount: {Amount} {Currency}",
            transfer.Id, transfer.TransactionCode, transfer.Amount, transfer.Currency);

        return Result<CreateTransferResponse>.Success(MapToCreateResponse(transfer));
    }

    public async Task<Result<TransferDto>> CompleteTransferAsync(string transactionCode, CompleteTransferRequest request)
    {
        var transfer = await _repository.GetByTransactionCodeAsync(transactionCode);

        if (transfer == null)
            return Result<TransferDto>.Failure("Transfer not found");

        // Use domain service for validation
        var validationResult = TransferValidator.ValidateTransferForCompletion(transfer, request.ReceiverNationalId);
        if (!validationResult.IsSuccess)
            return Result<TransferDto>.Failure(validationResult.Error!);

        // Use aggregate method to complete
        transfer.Complete();

        await _repository.UpdateAsync(transfer);

        // Publish integration event directly to RabbitMQ
        await _eventPublisher.PublishAsync(new TransferCompletedEvent
        {
            TransferId = transfer.Id,
            TransactionCode = transfer.TransactionCode
        });

        _logger.LogInformation("Transfer completed: {TransferId} - Code: {TransactionCode}",
            transfer.Id, transfer.TransactionCode);

        return Result<TransferDto>.Success(MapToDto(transfer));
    }

    public async Task<Result<TransferDto>> CancelTransferAsync(string transactionCode, CancelTransferRequest request)
    {
        var transfer = await _repository.GetByTransactionCodeAsync(transactionCode);

        if (transfer == null)
            return Result<TransferDto>.Failure("Transfer not found");

        // Use aggregate method to cancel
        transfer.Cancel(request.Reason);

        await _repository.UpdateAsync(transfer);

        // Publish integration event directly to RabbitMQ
        await _eventPublisher.PublishAsync(new TransferCancelledEvent
        {
            TransferId = transfer.Id,
            Reason = request.Reason ?? string.Empty
        });

        _logger.LogInformation("Transfer cancelled: {TransferId} - Reason: {Reason}",
            transfer.Id, request.Reason);

        return Result<TransferDto>.Success(MapToDto(transfer));
    }

    public async Task<Result<TransferDto>> GetTransferByCodeAsync(string transactionCode)
    {
        var transfer = await _repository.GetByTransactionCodeAsync(transactionCode);
        
        if (transfer == null)
            return Result<TransferDto>.Failure("Transfer not found");
        
        return Result<TransferDto>.Success(MapToDto(transfer));
    }

    public async Task<IEnumerable<TransferDto>> GetCustomerTransfersAsync(Guid customerId)
    {
        var transfers = await _repository.GetCustomerTransfersAsync(customerId);
        return transfers.Select(MapToDto);
    }

    public async Task<DailyLimitCheckResponse> CheckDailyLimitAsync(Guid customerId)
    {
        var totalToday = await _repository.GetDailyTotalAsync(customerId, DateTime.Today);

        return new DailyLimitCheckResponse
        {
            TotalTransfersToday = totalToday,
            DailyLimit = DAILY_LIMIT_TRY
        };
    }

    private async Task<string> GenerateUniqueTransactionCodeAsync()
    {
        string code;
        bool exists;

        do
        {
            code = TransactionCodeGenerator.Generate();
            exists = await _repository.TransactionCodeExistsAsync(code);
        }
        while (exists);

        return code;
    }

    private static CreateTransferResponse MapToCreateResponse(TransferEntity transfer)
    {
        return new CreateTransferResponse
        {
            TransferId = transfer.Id,
            TransactionCode = transfer.TransactionCode,
            Status = transfer.Status,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            AmountInTRY = transfer.AmountInTRY,
            TransactionFee = transfer.TransactionFee,
            RiskLevel = transfer.RiskLevel,
            ApprovalRequiredUntil = transfer.ApprovalRequiredUntil,
            Message = transfer.ApprovalRequiredUntil.HasValue
                ? "Transfer created. 5-minute approval wait required for high-value transfers."
                : "Transfer created successfully"
        };
    }

    private static TransferDto MapToDto(TransferEntity transfer)
    {
        return new TransferDto
        {
            Id = transfer.Id,
            SenderId = transfer.SenderId,
            ReceiverId = transfer.ReceiverId,
            SenderNationalId = transfer.SenderNationalId,
            ReceiverNationalId = transfer.ReceiverNationalId,
            Amount = transfer.Amount,
            Currency = transfer.Currency,
            AmountInTRY = transfer.AmountInTRY,
            ExchangeRate = transfer.ExchangeRate,
            TransactionFee = transfer.TransactionFee,
            TransactionCode = transfer.TransactionCode,
            Status = transfer.Status,
            RiskLevel = transfer.RiskLevel,
            CreatedAt = transfer.CreatedAt,
            CompletedAt = transfer.CompletedAt,
            CancelledAt = transfer.CancelledAt,
            CancellationReason = transfer.CancellationReason,
            ApprovalRequiredUntil = transfer.ApprovalRequiredUntil
        };
    }
}
