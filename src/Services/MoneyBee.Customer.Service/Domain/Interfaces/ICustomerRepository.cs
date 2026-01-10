using MoneyBee.Common.DDD;
using MoneyBee.Common.Enums;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<CustomerEntity?> GetByIdAsync(Guid id);
    Task<CustomerEntity?> GetByNationalIdAsync(string nationalId);
    Task<IEnumerable<CustomerEntity>> GetAllAsync(int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<CustomerEntity>> GetUnverifiedKycCustomersAsync(int hours = 24);
    Task<IEnumerable<CustomerEntity>> FindAsync(ISpecification<CustomerEntity> specification);
    Task<CustomerEntity> CreateAsync(CustomerEntity customer);
    Task<CustomerEntity> UpdateAsync(CustomerEntity customer);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsByNationalIdAsync(string nationalId);
    Task<int> GetTotalCountAsync();
}
