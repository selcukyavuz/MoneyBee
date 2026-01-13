using MoneyBee.Common.Enums;
using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Application.DTOs;
using MoneyBee.Customer.Service.Application.Interfaces;
using MoneyBee.Customer.Service.Constants;
using MoneyBee.Customer.Service.Domain.Interfaces;
using MoneyBee.Customer.Service.Domain.Validators;
using MoneyBee.Customer.Service.Infrastructure.ExternalServices;
using MoneyBee.Customer.Service.Infrastructure.Messaging;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Application.Services;

public class CustomerService(
    ICustomerRepository repository,
    IKycService kycService,
    IEventPublisher eventPublisher,
    ILogger<CustomerService> logger) : ICustomerService
{
    public async Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Check if customer already exists
        var existingCustomer = await repository.GetByNationalIdAsync(request.NationalId);
        if (existingCustomer is not null)
        {
            return Result<CustomerDto>.Conflict(ErrorMessages.Customer.AlreadyExists);
        }

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
        {
            return Result<CustomerDto>.Validation(validationResult.Error!);
        }

        // Perform KYC verification (non-blocking)
        var kycResult = await kycService.VerifyCustomerAsync(
            request.NationalId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth);

        if (kycResult.IsVerified)
        {
            customer.VerifyKyc();
        }
        else
        {
            logger.LogWarning("KYC verification failed for {NationalId}: {Message}. Customer will be created with unverified status.",
                customer.NationalId, kycResult.Message);
        }

        await repository.CreateAsync(customer);

        // Publish integration event directly to RabbitMQ
        await eventPublisher.PublishAsync(new CustomerCreatedEvent
        {
            CustomerId = customer.Id,
            NationalId = customer.NationalId,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email ?? string.Empty,
            Timestamp = DateTime.UtcNow
        });

        var dto = MapToDto(customer);
        
        var logMessage = kycResult.IsVerified 
            ? "Customer created with verified KYC" 
            : "Customer created with unverified KYC - verification will be retried";
        
        logger.LogInformation("{LogMessage}: {CustomerId} - {NationalId}", logMessage, customer.Id, customer.NationalId);

        return Result<CustomerDto>.Success(dto);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 50)
    {
        var customers = await repository.GetAllAsync(pageNumber, pageSize);
        return customers.Select(MapToDto);
    }

    public async Task<Result<CustomerDto>> GetCustomerByIdAsync(Guid id)
    {
        var customer = await repository.GetByIdAsync(id);
        
        if (customer is null)
        {
            return Result<CustomerDto>.NotFound(ErrorMessages.Customer.NotFound);
        }
        
        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> GetCustomerByNationalIdAsync(string nationalId)
    {
        var customer = await repository.GetByNationalIdAsync(nationalId);
        
        if (customer is null)
        {
            return Result<CustomerDto>.NotFound(ErrorMessages.Customer.NotFound);
        }
        
        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request)
    {
        var customer = await repository.GetByIdAsync(id);

        if (customer is null)
        {
            return Result<CustomerDto>.NotFound(ErrorMessages.Customer.NotFound);
        }

        // Use aggregate method to update information
        customer.UpdateInformation(
            request.FirstName ?? customer.FirstName,
            request.LastName ?? customer.LastName,
            request.PhoneNumber ?? customer.PhoneNumber,
            request.Address ?? customer.Address,
            request.Email ?? customer.Email);

        await repository.UpdateAsync(customer);

        logger.LogInformation("Customer updated: {CustomerId}", id);

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result> UpdateCustomerStatusAsync(Guid id, UpdateCustomerStatusRequest request)
    {
        var customer = await repository.GetByIdAsync(id);

        if (customer is null)
        {
            return Result.NotFound(ErrorMessages.Customer.NotFound);
        }

        var validationResult = CustomerValidator.ValidateCustomerUpdate(customer, request.Status);
        
        if (!validationResult.IsSuccess)
        {
            return Result.Validation(validationResult.Error!);
        }

        // Capture old status before update for event
        var oldStatus = customer.Status;

        // Use aggregate method to update status
        customer.UpdateStatus(request.Status);

        await repository.UpdateAsync(customer);

        logger.LogInformation("Customer status updated: {CustomerId} -> {NewStatus}. Reason: {Reason}", 
            id, request.Status, request.Reason);

        // Publish integration event directly to RabbitMQ
        await eventPublisher.PublishAsync(new CustomerStatusChangedEvent
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
        var customer = await repository.GetByIdAsync(id);
        if (customer is null)
        {
            return false;
        }

        // Mark for deletion and raise domain event
        customer.MarkForDeletion();

        var deleted = await repository.DeleteAsync(id);

        if (deleted)
        {
            logger.LogWarning("Customer deleted: {CustomerId}", id);

            // Publish integration event directly to RabbitMQ
            await eventPublisher.PublishAsync(new CustomerDeletedEvent
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
            var customer = await repository.GetByNationalIdAsync(nationalId);

            if (customer is null)
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
                    Status = customer.Status,
                    KycVerified = customer.KycVerified,
                    IsActive = false,
                    Message = $"Customer exists but status is {customer.Status}"
                };
            }

            return new CustomerVerificationResponse
            {
                Exists = true,
                CustomerId = customer.Id,
                Status = customer.Status,
                KycVerified = customer.KycVerified,
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
