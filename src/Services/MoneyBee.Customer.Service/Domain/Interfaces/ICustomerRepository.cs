using MoneyBee.Common.Enums;
using CustomerEntity = MoneyBee.Customer.Service.Domain.Entities.Customer;

namespace MoneyBee.Customer.Service.Domain.Interfaces;

/// <summary>
/// Repository interface for customer data access operations
/// </summary>
public interface ICustomerRepository
{
    /// <summary>
    /// Gets a customer by their unique identifier
    /// </summary>
    /// <param name="id">The customer's unique identifier</param>
    /// <returns>The customer entity if found, null otherwise</returns>
    Task<CustomerEntity?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Gets a customer by their national ID number
    /// </summary>
    /// <param name="nationalId">The national ID number</param>
    /// <returns>The customer entity if found, null otherwise</returns>
    Task<CustomerEntity?> GetByNationalIdAsync(string nationalId);
    
    /// <summary>
    /// Gets all customers with pagination
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 50)</param>
    /// <returns>Collection of customer entities</returns>
    Task<IEnumerable<CustomerEntity>> GetAllAsync(int pageNumber = 1, int pageSize = 50);
    
    /// <summary>
    /// Gets customers who haven't completed KYC verification within specified hours
    /// </summary>
    /// <param name="hours">The number of hours to look back (default: 24)</param>
    /// <returns>Collection of unverified customer entities</returns>
    Task<IEnumerable<CustomerEntity>> GetUnverifiedKycCustomersAsync(int hours = 24);
    
    /// <summary>
    /// Creates a new customer
    /// </summary>
    /// <param name="customer">The customer entity to create</param>
    /// <returns>The created customer entity</returns>
    Task<CustomerEntity> CreateAsync(CustomerEntity customer);
    
    /// <summary>
    /// Updates an existing customer
    /// </summary>
    /// <param name="customer">The customer entity with updated values</param>
    /// <returns>The updated customer entity</returns>
    Task<CustomerEntity> UpdateAsync(CustomerEntity customer);
    
    /// <summary>
    /// Deletes a customer by their unique identifier
    /// </summary>
    /// <param name="id">The customer's unique identifier</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id);
    
    /// <summary>
    /// Checks if a customer exists with the given national ID
    /// </summary>
    /// <param name="nationalId">The national ID to check</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsByNationalIdAsync(string nationalId);
    
    /// <summary>
    /// Gets the total count of customers in the system
    /// </summary>
    /// <returns>Total customer count</returns>
    Task<int> GetTotalCountAsync();
}
