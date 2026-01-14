using MoneyBee.Common.Abstractions;
using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.Application.Customers.Queries.GetAllCustomers;

/// <summary>
/// Handles getting all customers with pagination
/// </summary>
public class GetAllCustomersHandler(
    ICustomerRepository repository) : IQueryHandler<GetAllCustomersRequest, IEnumerable<CustomerDto>>
{
    public async Task<IEnumerable<CustomerDto>> HandleAsync(GetAllCustomersRequest request, CancellationToken cancellationToken = default)
    {
        var customers = await repository.GetAllAsync(request.PageNumber, request.PageSize);
        return customers.Select(MapToDto);
    }

    private static CustomerDto MapToDto(Domain.Customers.Customer customer)
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
