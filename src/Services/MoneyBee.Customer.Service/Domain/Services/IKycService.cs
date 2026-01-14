namespace MoneyBee.Customer.Service.Domain.Services;

/// <summary>
/// KYC (Know Your Customer) verification service interface.
/// </summary>
public interface IKycService
{
    /// <summary>
    /// Verifies a customer's identity against KYC database.
    /// </summary>
    /// <param name="nationalId">Customer's national identification number</param>
    /// <param name="firstName">Customer's first name</param>
    /// <param name="lastName">Customer's last name</param>
    /// <param name="dateOfBirth">Customer's date of birth</param>
    /// <returns>KYC verification result</returns>
    Task<KycVerificationResult> VerifyCustomerAsync(
        string nationalId,
        string firstName,
        string lastName,
        DateTime dateOfBirth);
}
