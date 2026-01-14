using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Application.Transfers.Services;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Infrastructure.Constants;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.FraudDetectionService;

public class FraudDetectionService(
    HttpClient httpClient,
    ILogger<FraudDetectionService> logger) : IFraudDetectionService
{

    public async Task<Result<FraudCheckResult>> CheckTransferAsync(
        Guid senderId,
        Guid receiverId,
        decimal amountInTRY,
        string senderNationalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new
            {
                senderId = senderId.ToString(),
                receiverId = receiverId.ToString(),
                amount = amountInTRY,
                nationalId = senderNationalId
            };

            logger.LogInformation("Calling Fraud Detection Service for transfer check");

            var response = await httpClient.PostAsJsonAsync(ExternalApiEndpoints.FraudDetection.CheckTransfer, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Fraud Service returned error: {StatusCode}", response.StatusCode);
                return Result<FraudCheckResult>.Failure(TransferErrors.FraudDetectionFailed);
            }

            var result = await response.Content.ReadFromJsonAsync<FraudCheckResult>();
            
            if (result is null)
            {
                logger.LogWarning("Fraud Service returned invalid response");
                return Result<FraudCheckResult>.Failure(TransferErrors.FraudDetectionInvalid);
            }

            logger.LogInformation("Fraud check result: {RiskLevel}", result.RiskLevel);
            return Result<FraudCheckResult>.Success(result);
        }
        catch (HttpRequestException ex)
        {
            // Graceful degradation: if fraud service is unavailable, default to Medium risk
            logger.LogWarning(ex, "Fraud Detection Service unavailable, defaulting to Medium risk");
            return Result<FraudCheckResult>.Success(new FraudCheckResult
            {
                RiskLevel = MoneyBee.Common.Enums.RiskLevel.Medium,
                Message = "Fraud service unavailable - defaulted to Medium risk"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Fraud Detection Service");
            return Result<FraudCheckResult>.Failure(TransferErrors.FraudDetectionError);
        }
    }
}
