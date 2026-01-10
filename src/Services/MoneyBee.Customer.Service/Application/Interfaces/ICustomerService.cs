using MoneyBee.Customer.Service.Application.DTOs;

namespace MoneyBee.Customer.Service.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 50);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id);
    Task<CustomerDto?> GetCustomerByNationalIdAsync(string nationalId);
    Task<CustomerDto?> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request);
    Task<bool> UpdateCustomerStatusAsync(Guid id, UpdateCustomerStatusRequest request);
    Task<bool> DeleteCustomerAsync(Guid id);
    Task<CustomerVerificationResponse> VerifyCustomerAsync(string nationalId);
}
