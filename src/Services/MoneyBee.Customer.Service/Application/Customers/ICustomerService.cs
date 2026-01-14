using MoneyBee.Common.Results;
using MoneyBee.Customer.Service.Application.Customers;

namespace MoneyBee.Customer.Service.Application.Customers;

/// <summary>
/// Service interface for customer management operations including KYC verification
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Creates a new customer with KYC verification
    /// </summary>
    /// <param name="request">The customer creation request</param>
    /// <returns>Result containing the created customer DTO</returns>
    Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerRequest request);
    
    /// <summary>
    /// Gets all customers with pagination
    /// </summary>
    /// <param name="pageNumber">The page number (default: 1)</param>
    /// <param name="pageSize">The page size (default: 50)</param>
    /// <returns>Collection of customer DTOs</returns>
    Task<IEnumerable<CustomerDto>> GetAllCustomersAsync(int pageNumber = 1, int pageSize = 50);
    
    /// <summary>
    /// Gets a customer by their unique identifier
    /// </summary>
    /// <param name="id">The customer's unique identifier</param>
    /// <returns>Result containing the customer DTO if found</returns>
    Task<Result<CustomerDto>> GetCustomerByIdAsync(Guid id);
    
    /// <summary>
    /// Gets a customer by their national ID number
    /// </summary>
    /// <param name="nationalId">The national ID number</param>
    /// <returns>Result containing the customer DTO if found</returns>
    Task<Result<CustomerDto>> GetCustomerByNationalIdAsync(string nationalId);
    
    /// <summary>
    /// Updates an existing customer
    /// </summary>
    /// <param name="id">The customer's unique identifier</param>
    /// <param name="request">The update request with new values</param>
    /// <returns>Result containing the updated customer DTO</returns>
    Task<Result<CustomerDto>> UpdateCustomerAsync(Guid id, UpdateCustomerRequest request);
    
    /// <summary>
    /// Updates a customer's status and publishes status change event
    /// </summary>
    /// <param name="id">The customer's unique identifier</param>
    /// <param name="request">The status update request</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> UpdateCustomerStatusAsync(Guid id, UpdateCustomerStatusRequest request);
    
    /// <summary>
    /// Deletes a customer from the system
    /// </summary>
    /// <param name="id">The customer's unique identifier</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteCustomerAsync(Guid id);
    
    /// <summary>
    /// Verifies a customer's identity by national ID
    /// </summary>
    /// <param name="nationalId">The national ID to verify</param>
    /// <returns>Customer verification response with status</returns>
    Task<CustomerVerificationResponse> VerifyCustomerAsync(string nationalId);
}
