using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerById;

/// <summary>
/// Handles getting customer by ID
/// </summary>
public class GetCustomerByIdHandler(
    ICustomerRepository repository) : IQueryHandler<Guid, Result<GetCustomerByIdResponse>>
{
    public async Task<Result<GetCustomerByIdResponse>> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(id);
        
        if (customer is null)
        {
            return Result<GetCustomerByIdResponse>.NotFound(ErrorMessages.NotFound);
        }
        
        return Result<GetCustomerByIdResponse>.Success(MapToDto(customer));
    }

    private static GetCustomerByIdResponse MapToDto(Domain.Customers.Customer customer)
    {
        return new GetCustomerByIdResponse
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
