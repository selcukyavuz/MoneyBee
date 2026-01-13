using MoneyBee.Common.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using System.Text.Json;

namespace MoneyBee.Customer.Service.Infrastructure.ExternalServices;

/// <summary>
/// Service interface for KYC (Know Your Customer) verification with external KYC provider
/// </summary>
public interface IKycService
{
    /// <summary>
    /// Verifies customer identity with external KYC service
    /// </summary>
    /// <param name="nationalId">The customer's national ID</param>
    /// <param name="firstName">The customer's first name</param>
    /// <param name="lastName">The customer's last name</param>
    /// <param name="dateOfBirth">The customer's date of birth</param>
    /// <returns>KYC verification result with verification status and risk score</returns>
    Task<KycVerificationResult> VerifyCustomerAsync(string nationalId, string firstName, string lastName, DateTime dateOfBirth);
}

/// <summary>
/// Represents the result of a KYC verification check
/// </summary>
public class KycVerificationResult
{
    public bool IsVerified { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RiskScore { get; set; }
}

public class KycService(
    IHttpClientFactory httpClientFactory,
    ILogger<KycService> logger,
    IConfiguration configuration) : IKycService
{
    private readonly HttpClient httpClient = CreateHttpClient(httpClientFactory, configuration);
    private readonly AsyncCircuitBreakerPolicy circuitBreakerPolicy = CreateCircuitBreakerPolicy(logger);

    private static HttpClient CreateHttpClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        var httpClient = httpClientFactory.CreateClient("KycService");
        httpClient.BaseAddress = new Uri(configuration[MoneyBee.Common.Constants.ConfigurationKeys.ExternalServices.KycService] ?? "http://kyc-service");
        return httpClient;
    }

    private static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(ILogger<KycService> logger)
    {
        // Circuit breaker: Open after 3 failures, stay open for 30 seconds
        return Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    logger.LogWarning("KYC Service circuit breaker opened for {Duration}s", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    logger.LogInformation("KYC Service circuit breaker reset");
                });
    }

    public async Task<KycVerificationResult> VerifyCustomerAsync(
        string nationalId,
        string firstName,
        string lastName,
        DateTime dateOfBirth)
    {
        try
        {
            return await circuitBreakerPolicy.ExecuteAsync(async () =>
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

                    // Don't fail the request, return unverified
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
            });
        }
        catch (BrokenCircuitException)
        {
            logger.LogError("KYC Service circuit breaker is open");
            return new KycVerificationResult
            {
                IsVerified = false,
                Message = "KYC service temporarily unavailable"
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
