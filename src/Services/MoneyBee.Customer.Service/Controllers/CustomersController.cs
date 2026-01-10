using Microsoft.AspNetCore.Mvc;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;

namespace MoneyBee.Customer.Service.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerService customerService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new customer with KYC verification
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var dto = await _customerService.CreateCustomerAsync(request);
            return CreatedAtAction(
                nameof(GetCustomer),
                new { id = dto.Id },
                ApiResponse<CustomerDto>.SuccessResponse(dto, "Customer created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CustomerDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CustomerDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var dto = await _customerService.GetCustomerByIdAsync(id);

        if (dto == null)
        {
            return NotFound(ApiResponse<CustomerDto>.ErrorResponse("Customer not found"));
        }

        return Ok(ApiResponse<CustomerDto>.SuccessResponse(dto));
    }

    /// <summary>
    /// Get all customers with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerDto>>), 200)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var dtos = await _customerService.GetAllCustomersAsync(pageNumber, pageSize);
        return Ok(ApiResponse<IEnumerable<CustomerDto>>.SuccessResponse(dtos));
    }

    /// <summary>
    /// Update customer details
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var dto = await _customerService.UpdateCustomerAsync(id, request);

        if (dto == null)
        {
            return NotFound(ApiResponse<CustomerDto>.ErrorResponse("Customer not found"));
        }

        return Ok(ApiResponse<CustomerDto>.SuccessResponse(dto, "Customer updated successfully"));
    }

    /// <summary>
    /// Update customer status (Active/Passive/Blocked)
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCustomerStatus(Guid id, [FromBody] UpdateCustomerStatusRequest request)
    {
        var success = await _customerService.UpdateCustomerStatusAsync(id, request);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Customer not found"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Customer status updated successfully"));
    }

    /// <summary>
    /// Delete customer
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var success = await _customerService.DeleteCustomerAsync(id);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Customer not found"));
        }

        return NoContent();
    }

    /// <summary>
    /// Verify customer by National ID
    /// </summary>
    [HttpGet("verify/{nationalId}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerVerificationResponse>), 200)]
    public async Task<IActionResult> VerifyCustomer(string nationalId)
    {
        var response = await _customerService.VerifyCustomerAsync(nationalId);
        return Ok(ApiResponse<CustomerVerificationResponse>.SuccessResponse(response));
    }
}
