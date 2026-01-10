using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Exceptions;
using MoneyBee.Common.Models;
using MoneyBee.Customer.Service.Data;
using MoneyBee.Customer.Service.DTOs;
using MoneyBee.Customer.Service.Helpers;
using MoneyBee.Customer.Service.Services;

namespace MoneyBee.Customer.Service.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerDbContext _context;
    private readonly IKycService _kycService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        CustomerDbContext context,
        IKycService kycService,
        IEventPublisher eventPublisher,
        ILogger<CustomersController> logger)
    {
        _context = context;
        _kycService = kycService;
        _eventPublisher = eventPublisher;
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
        // Validate National ID format
        var normalizedNationalId = NationalIdValidator.Normalize(request.NationalId);
        if (!NationalIdValidator.IsValid(normalizedNationalId))
        {
            return BadRequest(ApiResponse<CustomerDto>.ErrorResponse("Invalid National ID format"));
        }

        // Check age requirement (18+)
        var age = DateTime.Today.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
        
        if (age < 18)
        {
            return BadRequest(ApiResponse<CustomerDto>.ErrorResponse("Customer must be at least 18 years old"));
        }

        // Check if customer already exists
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.NationalId == normalizedNationalId);

        if (existingCustomer != null)
        {
            return BadRequest(ApiResponse<CustomerDto>.ErrorResponse("Customer with this National ID already exists"));
        }

        // Corporate customers must have tax number
        if (request.CustomerType == CustomerType.Corporate && string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            return BadRequest(ApiResponse<CustomerDto>.ErrorResponse("Tax number is required for corporate customers"));
        }

        // Perform KYC verification (non-blocking)
        var kycResult = await _kycService.VerifyCustomerAsync(
            normalizedNationalId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth);

        if (!kycResult.IsVerified)
        {
            _logger.LogWarning("KYC verification failed for {NationalId}: {Message}. Customer will be created with unverified status.",
                normalizedNationalId, kycResult.Message);
        }

        // Create customer (even if KYC fails)
        var customer = new Entities.Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            NationalId = normalizedNationalId,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc),
            CustomerType = request.CustomerType,
            Status = CustomerStatus.Active,
            KycVerified = kycResult.IsVerified,
            TaxNumber = request.TaxNumber,
            Address = request.Address,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var logMessage = kycResult.IsVerified 
            ? "Customer created with verified KYC" 
            : "Customer created with unverified KYC - verification will be retried";
        
        _logger.LogInformation("{LogMessage}: {CustomerId} - {NationalId}", logMessage, customer.Id, normalizedNationalId);

        var dto = MapToDto(customer);
        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = customer.Id },
            ApiResponse<CustomerDto>.SuccessResponse(dto, "Customer created successfully"));
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
        {
            return NotFound(ApiResponse<CustomerDto>.ErrorResponse("Customer not found"));
        }

        var dto = MapToDto(customer);
        return Ok(ApiResponse<CustomerDto>.SuccessResponse(dto));
    }

    /// <summary>
    /// Get all customers with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<CustomerDto>>), 200)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] CustomerStatus? status = null)
    {
        var query = _context.Customers.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = customers.Select(MapToDto).ToList();

        var pagedResponse = new PagedResponse<CustomerDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PagedResponse<CustomerDto>>.SuccessResponse(pagedResponse));
    }

    /// <summary>
    /// Update customer details
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
        {
            return NotFound(ApiResponse<CustomerDto>.ErrorResponse("Customer not found"));
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            customer.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName))
            customer.LastName = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            customer.PhoneNumber = request.PhoneNumber;

        if (request.Address != null)
            customer.Address = request.Address;

        if (request.Email != null)
            customer.Email = request.Email;

        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer updated: {CustomerId}", id);

        var dto = MapToDto(customer);
        return Ok(ApiResponse<CustomerDto>.SuccessResponse(dto, "Customer updated successfully"));
    }

    /// <summary>
    /// Update customer status (Active/Passive/Blocked)
    /// </summary>
    [HttpPatch("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<CustomerDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCustomerStatus(Guid id, [FromBody] UpdateCustomerStatusRequest request)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
        {
            return NotFound(ApiResponse<CustomerDto>.ErrorResponse("Customer not found"));
        }

        var previousStatus = customer.Status;
        customer.Status = request.Status;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer status changed: {CustomerId} from {PreviousStatus} to {NewStatus}",
            id, previousStatus, request.Status);

        // Publish event
        await _eventPublisher.PublishCustomerStatusChangedAsync(new CustomerStatusChangedEvent
        {
            CustomerId = customer.Id,
            PreviousStatus = previousStatus.ToString(),
            NewStatus = customer.Status.ToString(),
            Reason = request.Reason,
            CorrelationId = HttpContext.TraceIdentifier
        });

        var dto = MapToDto(customer);
        return Ok(ApiResponse<CustomerDto>.SuccessResponse(dto, "Customer status updated successfully"));
    }

    /// <summary>
    /// Verify customer by National ID
    /// </summary>
    [HttpGet("verify/{nationalId}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerVerificationResponse>), 200)]
    public async Task<IActionResult> VerifyCustomer(string nationalId)
    {
        var normalizedNationalId = NationalIdValidator.Normalize(nationalId);
        
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.NationalId == normalizedNationalId);

        var response = new CustomerVerificationResponse
        {
            Exists = customer != null,
            CustomerId = customer?.Id,
            Status = customer?.Status,
            KycVerified = customer?.KycVerified
        };

        return Ok(ApiResponse<CustomerVerificationResponse>.SuccessResponse(response));
    }

    private static CustomerDto MapToDto(Entities.Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            NationalId = customer.NationalId,
            PhoneNumber = customer.PhoneNumber,
            DateOfBirth = customer.DateOfBirth,
            CustomerType = customer.CustomerType,
            Status = customer.Status,
            KycVerified = customer.KycVerified,
            TaxNumber = customer.TaxNumber,
            Address = customer.Address,
            Email = customer.Email,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
