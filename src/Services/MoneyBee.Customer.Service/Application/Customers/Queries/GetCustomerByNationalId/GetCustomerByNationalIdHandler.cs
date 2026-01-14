using MoneyBee.Common.Abstractions;
using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.Application.Customers.Queries.GetCustomerByNationalId;

/// <summary>
/// Handles getting customer by national ID
/// </summary>
public class GetCustomerByNationalIdHandler(
    ICustomerRepository repository) : IQueryHandler<string, Result<GetCustomerByNationalIdResponse>>
{
    public async Task<Result<GetCustomerByNationalIdResponse>> HandleAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByNationalIdAsync(nationalId);
        
        if (customer is null)
        {
            return Result<GetCustomerByNationalIdResponse>.NotFound(ErrorMessages.NotFound);
        }
        
        return Result<GetCustomerByNationalIdResponse>.Success(MapToDto(customer));
    }

    private static GetCustomerByNationalIdResponse MapToDto(Domain.Customers.Customer customer)
    {
        return new GetCustomerByNationalIdResponse
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
