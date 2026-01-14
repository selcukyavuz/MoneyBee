using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.Application.Customers.Commands.UpdateCustomer;

/// <summary>
/// Handles customer information updates
/// </summary>
public class UpdateCustomerHandler(
    ICustomerRepository repository,
    ILogger<UpdateCustomerHandler> logger) : ICommandHandler<UpdateCustomerRequest, Result<UpdateCustomerResponse>>
{
    public async Task<Result<UpdateCustomerResponse>> HandleAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Get customer
        var customer = await repository.GetByIdAsync(request.Id);
        if (customer is null)
        {
            return Result<UpdateCustomerResponse>.NotFound(ErrorMessages.NotFound);
        }

        // 2. Update information using aggregate method
        customer.UpdateInformation(
            request.FirstName ?? customer.FirstName,
            request.LastName ?? customer.LastName,
            request.PhoneNumber ?? customer.PhoneNumber,
            request.Address ?? customer.Address,
            request.Email ?? customer.Email);

        // 3. Save changes
        await repository.UpdateAsync(customer);

        logger.LogInformation("Customer updated: {CustomerId}", request.Id);

        return Result<UpdateCustomerResponse>.Success(MapToDto(customer));
    }

    private static UpdateCustomerResponse MapToDto(Domain.Customers.Customer customer)
    {
        return new UpdateCustomerResponse
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
