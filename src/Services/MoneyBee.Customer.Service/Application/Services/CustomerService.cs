using System.Diagnostics;
using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;
using MoneyBee.Customer.Service.Domain.Interfaces;
using MoneyBee.Customer.Service.Domain.Validators;
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

    public async Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Check if customer already exists
        var existingCustomer = await _repository.GetByNationalIdAsync(request.NationalId);
        if (existingCustomer != null)
            return Result<CustomerDto>.Failure("Customer with this National ID already exists");

        // Create customer aggregate using factory method
        var customer = CustomerEntity.Create(
            request.FirstName,
            request.LastName,
            request.NationalId,
            request.PhoneNumber,
            DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc),
            request.CustomerType,
            request.TaxNumber,
            request.Address,
            request.Email);

        // Use domain service for validation
        var validationResult = CustomerValidator.ValidateCustomerForCreation(customer);
        if (!validationResult.IsSuccess)
            return Result<CustomerDto>.Failure(validationResult.Error!);

        // Perform KYC verification (non-blocking)
        var kycStopwatch = Stopwatch.StartNew();
        var kycResult = await _kycService.VerifyCustomerAsync(
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth);
        kycStopwatch.Stop();

        if (kycResult.IsVerified)
        {
            customer.VerifyKyc();
        }
        else
        {
            _logger.LogWarning("KYC verification failed for {NationalId}: {Message}. Customer will be created with unverified status.",
                customer.NationalId, kycResult.Message);
        }

        await _repository.CreateAsync(customer);

        // Publish integration event directly to RabbitMQ
        await _eventPublisher.PublishAsync(new CustomerCreatedEvent
        {
            CustomerId = customer.Id,
            NationalId = customer.NationalId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email ?? string.Empty,
            Timestamp = DateTime.UtcNow
        });

        var dto = MapToDto(customer);

        stopwatch.Stop();
        
        var logMessage = kycResult.IsVerified 
            ? "Customer created with verified KYC" 
            : "Customer created with unverified KYC - verification will be retried";
        
        _logger.LogInformation("{LogMessage}: {CustomerId} - {NationalId}", logMessage, customer.Id, customer.NationalId);

        return Result<CustomerDto>.Success(dto);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 50)
    {
        var stopwatch = Stopwatch.StartNew();
        var customers = await _repository.GetAllAsync(pageNumber, pageSize);
        stopwatch.Stop();
        return customers.Select(MapToDto);
    }

    public async Task<Result<CustomerDto>> GetCustomerByIdAsync(Guid id)
    {
        var stopwatch = Stopwatch.StartNew();
        var customer = await _repository.GetByIdAsync(id);
        stopwatch.Stop();
        
        if (customer == null)
            return Result<CustomerDto>.Failure("Customer not found");
        
        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> GetCustomerByNationalIdAsync(string nationalId)
    {
        var stopwatch = Stopwatch.StartNew();
        var customer = await _repository.GetByNationalIdAsync(nationalId);
        stopwatch.Stop();
        
        if (customer == null)
            return Result<CustomerDto>.Failure("Customer not found");
        
        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return Result<CustomerDto>.Failure("Customer not found");

        // Use aggregate method to update information
        customer.UpdateInformation(
            request.FirstName ?? customer.FirstName,
            request.LastName ?? customer.LastName,
            request.PhoneNumber ?? customer.PhoneNumber,
            request.Address ?? customer.Address,
            request.Email ?? customer.Email);

        await _repository.UpdateAsync(customer);

        stopwatch.Stop();

        _logger.LogInformation("Customer updated: {CustomerId}", id);

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result> UpdateCustomerStatusAsync(Guid id, UpdateCustomerStatusRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return Result.Failure("Customer not found");

        // Use domain service for validation
        var validationResult = CustomerValidator.ValidateCustomerUpdate(customer, request.Status);
        if (!validationResult.IsSuccess)
            return Result.Failure(validationResult.Error!);

        // Capture old status before update for event
        var oldStatus = customer.Status;

        // Use aggregate method to update status
        customer.UpdateStatus(request.Status);

        await _repository.UpdateAsync(customer);

        stopwatch.Stop();
        _logger.LogInformation("Customer status updated: {CustomerId} -> {NewStatus}. Reason: {Reason}", 
            id, request.Status, request.Reason);

        // Publish integration event directly to RabbitMQ
        await _eventPublisher.PublishAsync(new CustomerStatusChangedEvent
        {
            CustomerId = customer.Id,
            PreviousStatus = oldStatus.ToString(),
            NewStatus = customer.Status.ToString(),
            Reason = request.Reason ?? string.Empty
        });

        return Result.Success();
    }

    public async Task<bool> DeleteCustomerAsync(Guid id)
    {
        var stopwatch = Stopwatch.StartNew();
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return false;

        // Mark for deletion and raise domain event
        customer.MarkForDeletion();

        var deleted = await _repository.DeleteAsync(id);

        if (deleted)
        {
            stopwatch.Stop();
            _logger.LogWarning("Customer deleted: {CustomerId}", id);

            // Publish integration event directly to RabbitMQ
            await _eventPublisher.PublishAsync(new CustomerDeletedEvent
            {
                CustomerId = customer.Id,
                NationalId = customer.NationalId,
                Timestamp = DateTime.UtcNow
            });
        }

        return deleted;
    }

    public async Task<CustomerVerificationResponse> VerifyCustomerAsync(string nationalId)
    {
        try
        {
            var customer = await _repository.GetByNationalIdAsync(nationalId);

            if (customer == null)
            {
                return new CustomerVerificationResponse
                {
                    Exists = false,
                    Message = "Customer not found"
                };
            }

            // Use domain service to check if customer can send transfers
            var canSend = CustomerValidator.CanCustomerSendTransfer(customer);

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
