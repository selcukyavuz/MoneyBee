using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;
using MoneyBee.Customer.Service.Domain.Interfaces;
using MoneyBee.Customer.Service.Helpers;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.Messaging;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IKycService _kycService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository repository,
        IKycService kycService,
        IEventPublisher eventPublisher,
        ILogger<CustomerService> logger)
    {
        _repository = repository;
        _kycService = kycService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Validate National ID format
        var normalizedNationalId = NationalIdValidator.Normalize(request.NationalId);
        if (!NationalIdValidator.IsValid(normalizedNationalId))
        {
            throw new ArgumentException("Invalid National ID format");
        }

        // Check age requirement (18+)
        var age = DateTime.Today.Year - request.DateOfBirth.Year;
        if (request.DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
        
        if (age < 18)
        {
            throw new ArgumentException("Customer must be at least 18 years old");
        }

        // Check if customer already exists
        var existingCustomer = await _repository.GetByNationalIdAsync(normalizedNationalId);
        if (existingCustomer != null)
        {
            throw new InvalidOperationException("Customer with this National ID already exists");
        }

        // Corporate customers must have tax number
        if (request.CustomerType == CustomerType.Corporate && string.IsNullOrWhiteSpace(request.TaxNumber))
        {
            throw new ArgumentException("Tax number is required for corporate customers");
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

        // Create customer entity
        var customer = new CustomerEntity
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

        await _repository.CreateAsync(customer);

        var logMessage = kycResult.IsVerified 
            ? "Customer created with verified KYC" 
            : "Customer created with unverified KYC - verification will be retried";
        
        _logger.LogInformation("{LogMessage}: {CustomerId} - {NationalId}", logMessage, customer.Id, normalizedNationalId);

        return MapToDto(customer);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 50)
    {
        var customers = await _repository.GetAllAsync(pageNumber, pageSize);
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
    {
        var customer = await _repository.GetByIdAsync(id);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerDto?> GetCustomerByNationalIdAsync(string nationalId)
    {
        var normalized = NationalIdValidator.Normalize(nationalId);
        var customer = await _repository.GetByNationalIdAsync(normalized);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.FirstName))
            customer.FirstName = request.FirstName;

        if (!string.IsNullOrWhiteSpace(request.LastName))
            customer.LastName = request.LastName;

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            customer.PhoneNumber = request.PhoneNumber;

        if (!string.IsNullOrWhiteSpace(request.Address))
            customer.Address = request.Address;

        if (!string.IsNullOrWhiteSpace(request.Email))
            customer.Email = request.Email;

        customer.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(customer);

        _logger.LogInformation("Customer updated: {CustomerId}", id);

        return MapToDto(customer);
    }

    public async Task<bool> UpdateCustomerStatusAsync(Guid id, UpdateCustomerStatusRequest request)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return false;

        var oldStatus = customer.Status;
        customer.Status = request.Status;
        customer.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(customer);

        _logger.LogInformation("Customer status updated: {CustomerId} from {OldStatus} to {NewStatus}. Reason: {Reason}",
            id, oldStatus, request.Status, request.Reason);

        // Publish status changed event
        await _eventPublisher.PublishAsync(new CustomerStatusChangedEvent
        {
            CustomerId = customer.Id,
            PreviousStatus = oldStatus.ToString(),
            NewStatus = customer.Status.ToString(),
            Reason = request.Reason,
            CorrelationId = Guid.NewGuid().ToString()
        });

        return true;
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return false;

        var deleted = await _repository.DeleteAsync(id);

        if (deleted)
        {
            _logger.LogWarning("Customer deleted: {CustomerId}", id);
        }

        return deleted;
    }

    public async Task<CustomerVerificationResponse> VerifyCustomerAsync(string nationalId)
    {
        var normalized = NationalIdValidator.Normalize(nationalId);
        
        if (!NationalIdValidator.IsValid(normalized))
        {
            return new CustomerVerificationResponse
            {
                Exists = false,
                Message = "Invalid National ID format"
            };
        }

        var customer = await _repository.GetByNationalIdAsync(normalized);

        if (customer == null)
        {
            return new CustomerVerificationResponse
            {
                Exists = false,
                Message = "Customer not found"
            };
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return new CustomerVerificationResponse
            {
                Exists = true,
                CustomerId = customer.Id,
                IsActive = false,
                Message = $"Customer exists but status is {customer.Status}"
            };
        }

        return new CustomerVerificationResponse
        {
            Exists = true,
            CustomerId = customer.Id,
            IsActive = true,
            Message = "Customer found and active"
        };
    }

    private static CustomerDto MapToDto(CustomerEntity customer)
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
