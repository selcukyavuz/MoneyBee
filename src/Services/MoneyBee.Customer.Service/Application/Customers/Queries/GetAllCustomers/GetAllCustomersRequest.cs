using MoneyBee.Customer.Service.Domain.Customers;

namespace MoneyBee.Customer.Service.Application.Customers.Queries.GetAllCustomers;

/// <summary>
/// Request for paginated customer list
/// </summary>
public record GetAllCustomersRequest(int PageNumber = 1, int PageSize = 50);
