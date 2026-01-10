using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.ValueObjects;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;
using MoneyBee.Customer.Service.Domain.Interfaces;
using MoneyBee.Customer.Service.Domain.Services;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.Messaging;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly IKycService _kycService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly CustomerDomainService _domainService;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository repository,
        IKycService kycService,
        IEventPublisher eventPublisher,
        IDomainEventDispatcher domainEventDispatcher,
        CustomerDomainService domainService,
        ILogger<CustomerService> logger)
    {
        _repository = repository;
        _kycService = kycService;
        _eventPublisher = eventPublisher;
        _domainEventDispatcher = domainEventDispatcher;
        _domainService = domainService;
        _logger = logger;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Validate National ID using Value Object
        var nationalId = NationalId.Create(request.NationalId);

        // Check if customer already exists
        var existingCustomer = await _repository.GetByNationalIdAsync(nationalId.Value);
        if (existingCustomer != null)
        {
            throw new InvalidOperationException("Customer with this National ID already exists");
        }

        // Create customer aggregate using factory method
        var customer = CustomerEntity.Create(
            request.FirstName,
            request.LastName,
            nationalId.Value,
            request.PhoneNumber,
            DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc),
            request.CustomerType,
            request.TaxNumber,
            request.Address,
            request.Email);

        // Use domain service for validation
        _domainService.ValidateCustomerForCreation(customer);

        // Perform KYC verification (non-blocking)
        var kycResult = await _kycService.VerifyCustomerAsync(
            nationalId.Value,
            request.FirstName,
            request.LastName,
            request.DateOfBirth);

        if (kycResult.IsVerified)
        {
            customer.VerifyKyc();
        }
        else
        {
            _logger.LogWarning("KYC verification failed for {NationalId}: {Message}. Customer will be created with unverified status.",
                nationalId.Value, kycResult.Message);
        }

        await _repository.CreateAsync(customer);

        // Dispatch domain events to handlers
        await _domainEventDispatcher.DispatchAsync(customer.DomainEvents);
        customer.ClearDomainEvents();

        var logMessage = kycResult.IsVerified 
            ? "Customer created with verified KYC" 
            : "Customer created with unverified KYC - verification will be retried";
        
        _logger.LogInformation("{LogMessage}: {CustomerId} - {NationalId}", logMessage, customer.Id, nationalId.Value);

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
        var nationalIdVO = NationalId.Create(nationalId);
        var customer = await _repository.GetByNationalIdAsync(nationalIdVO.Value);
        return customer != null ? MapToDto(customer) : null;
    }

    public async Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return null;

        // Use aggregate method to update information
        customer.UpdateInformation(
            request.FirstName ?? customer.FirstName,
            request.LastName ?? customer.LastName,
            request.PhoneNumber ?? customer.PhoneNumber,
            request.Address ?? customer.Address,
            request.Email ?? customer.Email);

        await _repository.UpdateAsync(customer);

        _logger.LogInformation("Customer updated: {CustomerId}", id);

        return MapToDto(customer);
    }

    public async Task<bool> UpdateCustomerStatusAsync(Guid id, UpdateCustomerStatusRequest request)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return false;

        // Use domain service for validation
        _domainService.ValidateCustomerUpdate(customer, request.Status);

        // Use aggregate method to update status
        customer.UpdateStatus(request.Status);

        await _repository.UpdateAsync(customer);

        _logger.LogInformation("Customer status updated: {CustomerId} from {OldStatus} to {NewStatus}. Reason: {Reason}",
            id, customer.Status, request.Status, request.Reason);

        // Dispatch domain events
        await _domainEventDispatcher.DispatchAsync(customer.DomainEvents);
        customer.ClearDomainEvents();

        return true;
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return false;

        // Mark for deletion and raise domain event
        customer.MarkForDeletion();

        var deleted = await _repository.DeleteAsync(id);

        if (deleted)
        {
            _logger.LogWarning("Customer deleted: {CustomerId}", id);

            // Dispatch domain events
            await _domainEventDispatcher.DispatchAsync(customer.DomainEvents);
            customer.ClearDomainEvents();
        }

        return deleted;
    }

    public async Task<CustomerVerificationResponse> VerifyCustomerAsync(string nationalId)
    {
        // Use Value Object for validation
        try
        {
            var nationalIdVO = NationalId.Create(nationalId);
            var normalized = nationalIdVO.Value;

            var customer = await _repository.GetByNationalIdAsync(normalized);

            if (customer == null)
            {
                return new CustomerVerificationResponse
                {
                    Exists = false,
                    Message = "Customer not found"
                };
            }

            // Use domain service to check if customer can send transfers
            var canSend = _domainService.CanCustomerSendTransfer(customer);

            if (!canSend)
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
        catch (ArgumentException ex)
        {
            return new CustomerVerificationResponse
            {
                Exists = false,
                Message = ex.Message
            };
        }
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
