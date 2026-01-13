using MoneyBee.Common.Results;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.CustomerService;

/// <summary>
/// Service interface for communicating with Customer Service to verify customer information
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Gets customer information by national ID from Customer Service
    /// </summary>
    /// <param name="nationalId">The customer's national ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing customer information if found</returns>
    Task<Result<CustomerInfo>> GetCustomerByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);
}
