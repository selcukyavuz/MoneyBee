using MoneyBee.Common.Enums;
using MoneyBee.Common.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using System.Text;
using System.Text.Json;

namespace MoneyBee.Transfer.Service.Services;

public interface IFraudDetectionService
{
    Task<FraudCheckResult> CheckTransferAsync(Guid senderId, Guid receiverId, decimal amountInTRY, string senderNationalId);
}

public class FraudCheckResult
{
    public RiskLevel RiskLevel { get; set; }
    public bool IsApproved => RiskLevel == RiskLevel.Low;
    public string Message { get; set; } = string.Empty;
}

public class FraudDetectionService : IFraudDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FraudDetectionService> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _circuitBreakerPipeline;

    public FraudDetectionService(
        IHttpClientFactory httpClientFactory,
        ILogger<FraudDetectionService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("FraudService");
        _httpClient.BaseAddress = new Uri(configuration["ExternalServices:FraudService"] ?? "http://fraud-service");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;

        _circuitBreakerPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => !r.IsSuccessStatusCode)
            })
            .Build();
    }

    public async Task<FraudCheckResult> CheckTransferAsync(
        Guid senderId,
        Guid receiverId,
        decimal amountInTRY,
        string senderNationalId)
    {
        try
        {
            var response = await _circuitBreakerPipeline.ExecuteAsync(async ct =>
            {
                var request = new
                {
                    senderId = senderId.ToString(),
                    receiverId = receiverId.ToString(),
                    amount = amountInTRY,
                    nationalId = senderNationalId
                };

                _logger.LogInformation("Calling Fraud Detection Service for transfer check");

                return await _httpClient.PostAsJsonAsync("/api/fraud/check", request, ct);
            }, CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Fraud Service returned error: {StatusCode}", response.StatusCode);
                
                // Fail open with Medium risk
                return new FraudCheckResult
                {
                    RiskLevel = RiskLevel.Medium,
                    Message = "Fraud service unavailable, proceeding with caution"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<FraudCheckResult>();
            
            _logger.LogInformation("Fraud check result: {RiskLevel}", result?.RiskLevel ?? RiskLevel.Medium);

            return result ?? new FraudCheckResult
            {
                RiskLevel = RiskLevel.Medium,
                Message = "Invalid response from fraud service"
            };
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Fraud Service circuit breaker is open");
            return new FraudCheckResult
            {
                RiskLevel = RiskLevel.Medium,
                Message = "Fraud service temporarily unavailable"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Fraud Detection Service");
            return new FraudCheckResult
            {
                RiskLevel = RiskLevel.Medium,
                Message = "Error checking fraud"
            };
        }
    }
}
