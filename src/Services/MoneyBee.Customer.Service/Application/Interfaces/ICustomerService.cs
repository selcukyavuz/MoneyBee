using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Application.DTOs;

namespace MoneyBee.Customer.Service.Application.Interfaces;

public interface ICustomerService
{
    Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request);
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 50);
    Task<Result<CustomerDto>> GetCustomerByIdAsync(Guid id);
    Task<Result<CustomerDto>> GetCustomerByNationalIdAsync(string nationalId);
    Task<Result<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request);
    Task<Result> UpdateCustomerStatusAsync(Guid id, UpdateCustomerStatusRequest request);
    Task<bool> DeleteCustomerAsync(Guid id);
    Task<CustomerVerificationResponse> VerifyCustomerAsync(string nationalId);
}
