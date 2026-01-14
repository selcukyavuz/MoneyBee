using System.Text.Json;
using MoneyBee.Customer.Service.Domain.Services;

namespace MoneyBee.Customer.Service.Infrastructure.ExternalServices;

public class KycService(
    HttpClient httpClient,
    ILogger<KycService> logger) : IKycService
{
    public async Task<KycVerificationResult> VerifyCustomerAsync(
        string nationalId,
        string firstName,
        string lastName,
        DateTime dateOfBirth)
    {
        try
        {
            var request = new
            {
                nationalId,
                firstName,
                lastName,
                dateOfBirth = dateOfBirth.ToString("yyyy-MM-dd")
            };

            logger.LogInformation("Calling KYC Service for customer verification: {NationalId}", nationalId);

            var response = await httpClient.PostAsJsonAsync("/api/kyc/verify", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                logger.LogWarning("KYC Service returned error: {StatusCode} - {Content}",
                    response.StatusCode, errorContent);

                return new KycVerificationResult
                {
                    IsVerified = false,
                    Message = "KYC verification service unavailable"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<KycVerificationResult>();
            
            logger.LogInformation("KYC verification result for {NationalId}: {IsVerified}",
                nationalId, result?.IsVerified ?? false);

            return result ?? new KycVerificationResult
            {
                IsVerified = false,
                Message = "Invalid response from KYC service"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling KYC Service");
            return new KycVerificationResult
            {
                IsVerified = false,
                Message = "Error verifying customer"
            };
        }
    }
}
