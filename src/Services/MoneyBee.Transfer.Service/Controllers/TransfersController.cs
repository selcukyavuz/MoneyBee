using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Transfer.Service.Application.DTOs;
using MoneyBee.Transfer.Service.Application.Interfaces;

namespace MoneyBee.Transfer.Service.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransfersController : ControllerBase
{
    private readonly ITransferService _transferService;
    private readonly ILogger<TransfersController> _logger;

    public TransfersController(
        ITransferService transferService,
        ILogger<TransfersController> logger)
    {
        _transferService = transferService;
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
        try
        {
            var response = await _transferService.CreateTransferAsync(request);
            return CreatedAtAction(
                nameof(GetTransferByCode),
                new { code = response.TransactionCode },
                ApiResponse<CreateTransferResponse>.SuccessResponse(response, response.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CreateTransferResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transfer");
            return StatusCode(500, ApiResponse<CreateTransferResponse>.ErrorResponse("An error occurred while processing the transfer"));
        }
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
        try
        {
            var dto = await _transferService.CompleteTransferAsync(code, request);
            return Ok(ApiResponse<TransferDto>.SuccessResponse(dto, "Transfer completed successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing transfer");
            return StatusCode(500, ApiResponse<TransferDto>.ErrorResponse("An error occurred while completing the transfer"));
        }
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
        try
        {
            var dto = await _transferService.CancelTransferAsync(code, request);
            return Ok(ApiResponse<TransferDto>.SuccessResponse(dto, "Transfer cancelled. Fee will be refunded."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TransferDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer");
            return StatusCode(500, ApiResponse<TransferDto>.ErrorResponse("An error occurred while cancelling the transfer"));
        }
    }

    /// <summary>
    /// Get transfer by transaction code
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ApiResponse<TransferDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTransferByCode(string code)
    {
        var dto = await _transferService.GetTransferByCodeAsync(code);

        if (dto == null)
        {
            return NotFound(ApiResponse<TransferDto>.ErrorResponse("Transfer not found"));
        }

        return Ok(ApiResponse<TransferDto>.SuccessResponse(dto));
    }

    /// <summary>
    /// Get customer transfers
    /// </summary>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<List<TransferDto>>), 200)]
    public async Task<IActionResult> GetCustomerTransfers(Guid customerId)
    {
        var dtos = await _transferService.GetCustomerTransfersAsync(customerId);
        return Ok(ApiResponse<List<TransferDto>>.SuccessResponse(dtos.ToList()));
    }

    /// <summary>
    /// Check daily limit for customer
    /// </summary>
    [HttpGet("daily-limit/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<DailyLimitCheckResponse>), 200)]
    public async Task<IActionResult> CheckDailyLimit(Guid customerId)
    {
        var limitInfo = await _transferService.CheckDailyLimitAsync(customerId);
        return Ok(ApiResponse<DailyLimitCheckResponse>.SuccessResponse(limitInfo));
    }
}
