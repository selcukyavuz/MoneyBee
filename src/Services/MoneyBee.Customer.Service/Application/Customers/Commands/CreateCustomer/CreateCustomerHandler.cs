using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Common.Abstractions;
using MoneyBee.Customer.Service.Domain.Customers;
using MoneyBee.Customer.Service.Domain.Services;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Customers.Customer;

namespace MoneyBee.Customer.Service.Application.Customers.Commands.CreateCustomer;

/// <summary>
/// Handles customer creation with KYC verification
/// </summary>
public class CreateCustomerHandler(
    ICustomerRepository repository,
    IKycService kycService,
    IEventPublisher eventPublisher,
    ILogger<CreateCustomerHandler> logger) : ICommandHandler<CreateCustomerRequest, Result<CreateCustomerResponse>>
{
    public async Task<Result<CreateCustomerResponse>> HandleAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Check if customer already exists
        var existingCustomer = await repository.GetByNationalIdAsync(request.NationalId);
        if (existingCustomer is not null)
        {
            return Result<CreateCustomerResponse>.Conflict(ErrorMessages.AlreadyExists);
        }

        // 2. Create customer aggregate using factory method
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

        // 3. Use domain service for validation
        var validationResult = CustomerValidator.ValidateCustomerForCreation(customer);
        if (!validationResult.IsSuccess)
        {
            return Result<CreateCustomerResponse>.Validation(validationResult.Error!);
        }

        // 4. Perform KYC verification (non-blocking)
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

        // 5. Save customer
        await repository.CreateAsync(customer);

        // 6. Publish integration event
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

        return Result<CreateCustomerResponse>.Success(dto);
    }

    private static CreateCustomerResponse MapToDto(CustomerEntity customer)
    {
        return new CreateCustomerResponse
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
