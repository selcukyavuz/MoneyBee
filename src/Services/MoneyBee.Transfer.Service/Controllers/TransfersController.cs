using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Exceptions;
using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Data;
using MoneyBee.Transfer.Service.DTOs;
using MoneyBee.Transfer.Service.Helpers;
using MoneyBee.Transfer.Service.Services;

namespace MoneyBee.Transfer.Service.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransfersController : ControllerBase
{
    private readonly TransferDbContext _context;
    private readonly ICustomerService _customerService;
    private readonly IFraudDetectionService _fraudService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<TransfersController> _logger;

    private const decimal DAILY_LIMIT_TRY = 10000m;
    private const decimal HIGH_AMOUNT_THRESHOLD_TRY = 1000m;

    public TransfersController(
        TransferDbContext context,
        ICustomerService customerService,
        IFraudDetectionService fraudService,
        IExchangeRateService exchangeRateService,
        ILogger<TransfersController> logger)
    {
        _context = context;
        _customerService = customerService;
        _fraudService = fraudService;
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new transfer (Money Sending)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateTransferResponse>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateTransfer([FromBody] CreateTransferRequest request)
    {
        // Check idempotency
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingTransfer = await _context.Transfers
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey);

            if (existingTransfer != null)
            {
                _logger.LogInformation("Idempotent request detected: {IdempotencyKey}", request.IdempotencyKey);
                return Ok(ApiResponse<CreateTransferResponse>.SuccessResponse(MapToCreateResponse(existingTransfer)));
            }
        }

        // Validate amount
        if (request.Amount <= 0)
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse("Amount must be greater than zero"));
        }

        // Get sender customer
        var sender = await _customerService.GetCustomerByNationalIdAsync(request.SenderNationalId);
        if (sender == null)
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse("Sender customer not found"));
        }

        if (sender.Status != CustomerStatus.Active)
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse("Sender customer is not active"));
        }

        // Get receiver customer
        var receiver = await _customerService.GetCustomerByNationalIdAsync(request.ReceiverNationalId);
        if (receiver == null)
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse("Receiver customer not found"));
        }

        if (receiver.Status == CustomerStatus.Blocked)
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse("Receiver customer is blocked"));
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
                return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse(
                    "Exchange rate service unavailable. Please try again later."));
            }
        }
        else
        {
            amountInTRY = request.Amount;
        }

        // Check daily limit
        var dailyLimit = await CheckDailyLimitAsync(sender.Id, amountInTRY);
        if (!dailyLimit.CanTransfer(amountInTRY))
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse(
                $"Daily transfer limit exceeded. Remaining: {dailyLimit.RemainingLimit:F2} TRY"));
        }

        // Perform fraud check
        var fraudCheck = await _fraudService.CheckTransferAsync(
            sender.Id, receiver.Id, amountInTRY, sender.NationalId);

        // Reject if HIGH risk
        if (fraudCheck.RiskLevel == RiskLevel.High)
        {
            _logger.LogWarning("Transfer rejected due to HIGH risk: Sender={SenderId}", sender.Id);
            
            var rejectedTransfer = new Entities.Transfer
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

            _context.Transfers.Add(rejectedTransfer);
            await _context.SaveChangesAsync();

            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse(
                "Transfer rejected due to high fraud risk"));
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
        var transfer = new Entities.Transfer
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

        _context.Transfers.Add(transfer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer created: {TransferId} - Code: {TransactionCode}, Amount: {Amount} {Currency}",
            transfer.Id, transfer.TransactionCode, transfer.Amount, transfer.Currency);

        var response = MapToCreateResponse(transfer);
        return CreatedAtAction(
            nameof(GetTransferByCode),
            new { code = transfer.TransactionCode },
            ApiResponse<CreateTransferResponse>.SuccessResponse(response, "Transfer created successfully"));
    }

    /// <summary>
    /// Complete a transfer (Money Receiving)
    /// </summary>
    [HttpPost("{code}/complete")]
    [ProducesResponseType(typeof(ApiResponse<TransferDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CompleteTransfer(string code, [FromBody] CompleteTransferRequest request)
    {
        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(t => t.TransactionCode == code);

        if (transfer == null)
        {
            return NotFound(ApiResponse<TransferDto>.ErrorResponse("Transfer not found"));
        }

        if (transfer.Status != TransferStatus.Pending)
        {
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse(
                $"Transfer cannot be completed. Status: {transfer.Status}"));
        }

        // Verify receiver identity
        if (transfer.ReceiverNationalId != request.ReceiverNationalId)
        {
            _logger.LogWarning("Identity verification failed for transfer completion: {TransferId}", transfer.Id);
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse("Receiver identity verification failed"));
        }

        // Check if approval wait period is over
        if (transfer.ApprovalRequiredUntil.HasValue && transfer.ApprovalRequiredUntil.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (transfer.ApprovalRequiredUntil.Value - DateTime.UtcNow).TotalMinutes;
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse(
                $"Transfer approval required. Please wait {Math.Ceiling(remainingMinutes)} more minute(s)"));
        }

        transfer.Status = TransferStatus.Completed;
        transfer.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer completed: {TransferId} - Code: {TransactionCode}",
            transfer.Id, transfer.TransactionCode);

        var dto = MapToDto(transfer);
        return Ok(ApiResponse<TransferDto>.SuccessResponse(dto, "Transfer completed successfully"));
    }

    /// <summary>
    /// Cancel a transfer
    /// </summary>
    [HttpPost("{code}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<TransferDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CancelTransfer(string code, [FromBody] CancelTransferRequest request)
    {
        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(t => t.TransactionCode == code);

        if (transfer == null)
        {
            return NotFound(ApiResponse<TransferDto>.ErrorResponse("Transfer not found"));
        }

        if (transfer.Status != TransferStatus.Pending)
        {
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse(
                $"Transfer cannot be cancelled. Status: {transfer.Status}"));
        }

        transfer.Status = TransferStatus.Cancelled;
        transfer.CancelledAt = DateTime.UtcNow;
        transfer.CancellationReason = request.Reason;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Transfer cancelled: {TransferId} - Reason: {Reason}",
            transfer.Id, request.Reason);

        var dto = MapToDto(transfer);
        return Ok(ApiResponse<TransferDto>.SuccessResponse(dto, "Transfer cancelled. Fee will be refunded."));
    }

    /// <summary>
    /// Get transfer by transaction code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ApiResponse<TransferDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTransferByCode(string code)
    {
        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(t => t.TransactionCode == code);

        if (transfer == null)
        {
            return NotFound(ApiResponse<TransferDto>.ErrorResponse("Transfer not found"));
        }

        var dto = MapToDto(transfer);
        return Ok(ApiResponse<TransferDto>.SuccessResponse(dto));
    }

    /// <summary>
    /// Get customer transfers
    /// </summary>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<List<TransferDto>>), 200)]
    public async Task<IActionResult> GetCustomerTransfers(Guid customerId)
    {
        var transfers = await _context.Transfers
            .Where(t => t.SenderId == customerId || t.ReceiverId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync();

        var dtos = transfers.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<TransferDto>>.SuccessResponse(dtos));
    }

    /// <summary>
    /// Check daily limit for customer
    /// </summary>
    [HttpGet("daily-limit/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<DailyLimitCheckResponse>), 200)]
    public async Task<IActionResult> CheckDailyLimit(Guid customerId)
    {
        var limitInfo = await CheckDailyLimitAsync(customerId, 0);
        return Ok(ApiResponse<DailyLimitCheckResponse>.SuccessResponse(limitInfo));
    }

    private async Task<DailyLimitCheckResponse> CheckDailyLimitAsync(Guid customerId, decimal additionalAmount)
    {
        var today = DateTime.Today;
        var totalToday = await _context.Transfers
            .Where(t => t.SenderId == customerId &&
                       t.CreatedAt >= today &&
                       (t.Status == TransferStatus.Pending || t.Status == TransferStatus.Completed))
            .SumAsync(t => t.AmountInTRY);

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
            exists = await _context.Transfers.AnyAsync(t => t.TransactionCode == code);
        }
        while (exists);

        return code;
    }

    private static CreateTransferResponse MapToCreateResponse(Entities.Transfer transfer)
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

    private static TransferDto MapToDto(Entities.Transfer transfer)
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
