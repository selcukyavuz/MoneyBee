using MoneyBee.Common.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using System.Text.Json;

namespace MoneyBee.Customer.Service.Services;

public interface IKycService
{
    Task<KycVerificationResult> VerifyCustomerAsync(string nationalId, string firstName, string lastName, DateTime dateOfBirth);
}

public class KycVerificationResult
{
    public bool IsVerified { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? RiskScore { get; set; }
}

public class KycService : IKycService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KycService> _logger;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public KycService(
        IHttpClientFactory httpClientFactory,
        ILogger<KycService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("KycService");
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:KycService"] ?? "http://kyc-service");
        _logger = logger;

        // Circuit breaker: Open after 3 failures, stay open for 30 seconds
        _circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    _logger.LogWarning("KYC Service circuit breaker opened for {Duration}s", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    _logger.LogInformation("KYC Service circuit breaker reset");
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
            return await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                var request = new
                {
                    nationalId,
                    firstName,
                    lastName,
                    dateOfBirth = dateOfBirth.ToString("yyyy-MM-dd")
                };

                _logger.LogInformation("Calling KYC Service for customer verification: {NationalId}", nationalId);

                var response = await _httpClient.PostAsJsonAsync("/api/kyc/verify", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("KYC Service returned error: {StatusCode} - {Content}",
                        response.StatusCode, errorContent);

                    // Don't fail the request, return unverified
                    return new KycVerificationResult
                    {
                        IsVerified = false,
                        Message = "KYC verification service unavailable"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<KycVerificationResult>();
                
                _logger.LogInformation("KYC verification result for {NationalId}: {IsVerified}",
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
            _logger.LogError("KYC Service circuit breaker is open");
            return new KycVerificationResult
            {
                IsVerified = false,
                Message = "KYC service temporarily unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling KYC Service");
            return new KycVerificationResult
            {
                IsVerified = false,
                Message = "Error verifying customer"
            };
        }
    }
}
