using MoneyBee.Common.Enums;
using MoneyBee.Common.Exceptions;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Application.Interfaces;
using TransferEntity = MoneyBee.Transfer.Service.Domain.Entities.Transfer;
using MoneyBee.Transfer.Service.Domain.Interfaces;
using MoneyBee.Transfer.Service.Helpers;
using MoneyBee.Transfer.Service.Infrastructure.ExternalServices;

namespace MoneyBee.Transfer.Service.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRepository _repository;
    private readonly ICustomerService _customerService;
    private readonly IFraudDetectionService _fraudService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<TransferService> _logger;

    private const decimal DAILY_LIMIT_TRY = 10000m;
    private const decimal HIGH_AMOUNT_THRESHOLD_TRY = 1000m;

    public TransferService(
        ITransferRepository repository,
        ICustomerService customerService,
        IFraudDetectionService fraudService,
        IExchangeRateService exchangeRateService,
        ILogger<TransferService> logger)
    {
        _repository = repository;
        _customerService = customerService;
        _fraudService = fraudService;
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    public async Task<CreateTransferResponse> CreateTransferAsync(CreateTransferRequest request)
    {
        // Check idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingTransfer = await _repository.GetByIdempotencyKeyAsync(request.IdempotencyKey);
            if (existingTransfer != null)
            {
                _logger.LogInformation("Idempotent request detected: {IdempotencyKey}", request.IdempotencyKey);
                return MapToCreateResponse(existingTransfer);
            }
        }

        // Validate amount
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero");
        }

        // Get sender customer
        var sender = await _customerService.GetCustomerByNationalIdAsync(request.SenderNationalId);
        if (sender == null)
        {
            throw new ArgumentException("Sender customer not found");
        }

        if (sender.Status != CustomerStatus.Active)
        {
            throw new ArgumentException("Sender customer is not active");
        }

        // Get receiver customer
        var receiver = await _customerService.GetCustomerByNationalIdAsync(request.ReceiverNationalId);
        if (receiver == null)
        {
            throw new ArgumentException("Receiver customer not found");
        }

        if (receiver.Status == CustomerStatus.Blocked)
        {
            throw new ArgumentException("Receiver customer is blocked");
        }

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
                throw new InvalidOperationException("Exchange rate service unavailable. Please try again later.", ex);
            }
        }
        else
        {
            amountInTRY = request.Amount;
        }

        // Check daily limit
        var dailyTotal = await _repository.GetDailyTotalAsync(sender.Id, DateTime.Today);
        var remainingLimit = DAILY_LIMIT_TRY - dailyTotal;
        
        if (remainingLimit < amountInTRY)
        {
            throw new InvalidOperationException($"Daily transfer limit exceeded. Remaining: {remainingLimit:F2} TRY");
        }

        // Perform fraud check
        var fraudCheck = await _fraudService.CheckTransferAsync(
            sender.Id, receiver.Id, amountInTRY, sender.NationalId);

        // Reject if HIGH risk
        if (fraudCheck.RiskLevel == RiskLevel.High)
        {
            _logger.LogWarning("Transfer rejected due to HIGH risk: Sender={SenderId}", sender.Id);
            
            var rejectedTransfer = new TransferEntity
            {
                Id = Guid.NewGuid(),
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Amount = request.Amount,
                Currency = request.Currency,
                AmountInTRY = amountInTRY,
                ExchangeRate = exchangeRate,
                TransactionFee = 0,
                TransactionCode = TransactionCodeGenerator.Generate(),
                Status = TransferStatus.Failed,
                RiskLevel = fraudCheck.RiskLevel,
                IdempotencyKey = request.IdempotencyKey,
                SenderNationalId = sender.NationalId,
                ReceiverNationalId = receiver.NationalId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.CreateAsync(rejectedTransfer);

            throw new InvalidOperationException("Transfer rejected due to high fraud risk");
        }

        // Calculate fee
        var transactionFee = FeeCalculator.Calculate(amountInTRY);

        // Determine if approval wait is needed (>1000 TRY)
        DateTime? approvalRequiredUntil = null;
        if (amountInTRY > HIGH_AMOUNT_THRESHOLD_TRY)
        {
            approvalRequiredUntil = DateTime.UtcNow.AddMinutes(5);
        }

        // Create transfer
        var transfer = new TransferEntity
        {
            Id = Guid.NewGuid(),
            SenderId = sender.Id,
            ReceiverId = receiver.Id,
            Amount = request.Amount,
            Currency = request.Currency,
            AmountInTRY = amountInTRY,
            ExchangeRate = exchangeRate,
            TransactionFee = transactionFee,
            TransactionCode = await GenerateUniqueTransactionCodeAsync(),
            Status = TransferStatus.Pending,
            RiskLevel = fraudCheck.RiskLevel,
            IdempotencyKey = request.IdempotencyKey,
            ApprovalRequiredUntil = approvalRequiredUntil,
            SenderNationalId = sender.NationalId,
            ReceiverNationalId = receiver.NationalId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(transfer);

        _logger.LogInformation("Transfer created: {TransferId} - Code: {TransactionCode}, Amount: {Amount} {Currency}",
            transfer.Id, transfer.TransactionCode, transfer.Amount, transfer.Currency);

        return MapToCreateResponse(transfer);
    }

    public async Task<TransferDto> CompleteTransferAsync(string transactionCode, CompleteTransferRequest request)
    {
        var transfer = await _repository.GetByTransactionCodeAsync(transactionCode);

        if (transfer == null)
        {
            throw new ArgumentException("Transfer not found");
        }

        if (transfer.Status != TransferStatus.Pending)
        {
            throw new InvalidOperationException($"Transfer cannot be completed. Status: {transfer.Status}");
        }

        // Verify receiver identity
        if (transfer.ReceiverNationalId != request.ReceiverNationalId)
        {
            _logger.LogWarning("Identity verification failed for transfer completion: {TransferId}", transfer.Id);
            throw new ArgumentException("Receiver identity verification failed");
        }

        // Check if approval wait period is over
        if (transfer.ApprovalRequiredUntil.HasValue && transfer.ApprovalRequiredUntil.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (transfer.ApprovalRequiredUntil.Value - DateTime.UtcNow).TotalMinutes;
            throw new InvalidOperationException(
                $"Transfer approval required. Please wait {Math.Ceiling(remainingMinutes)} more minute(s)");
        }

        transfer.Status = TransferStatus.Completed;
        transfer.CompletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(transfer);

        _logger.LogInformation("Transfer completed: {TransferId} - Code: {TransactionCode}",
            transfer.Id, transfer.TransactionCode);

        return MapToDto(transfer);
    }

    public async Task<TransferDto> CancelTransferAsync(string transactionCode, CancelTransferRequest request)
    {
        var transfer = await _repository.GetByTransactionCodeAsync(transactionCode);

        if (transfer == null)
        {
            throw new ArgumentException("Transfer not found");
        }

        if (transfer.Status != TransferStatus.Pending)
        {
            throw new InvalidOperationException($"Transfer cannot be cancelled. Status: {transfer.Status}");
        }

        transfer.Status = TransferStatus.Cancelled;
        transfer.CancelledAt = DateTime.UtcNow;
        transfer.CancellationReason = request.Reason;

        await _repository.UpdateAsync(transfer);

        _logger.LogInformation("Transfer cancelled: {TransferId} - Reason: {Reason}",
            transfer.Id, request.Reason);

        return MapToDto(transfer);
    }

    public async Task<TransferDto?> GetTransferByCodeAsync(string transactionCode)
    {
        var transfer = await _repository.GetByTransactionCodeAsync(transactionCode);
        return transfer != null ? MapToDto(transfer) : null;
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
