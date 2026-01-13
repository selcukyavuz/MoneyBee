using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Constants;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.FraudDetectionService;

public class FraudDetectionService(
    HttpClient httpClient,
    ILogger<FraudDetectionService> logger) : IFraudDetectionService
{

    public async Task<Result<FraudCheckResult>> CheckTransferAsync(
        Guid senderId,
        Guid receiverId,
        decimal amountInTRY,
        string senderNationalId)
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

            var response = await httpClient.PostAsJsonAsync(ExternalApiEndpoints.FraudDetection.CheckTransfer, request, CancellationToken.None);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Fraud Service returned error: {StatusCode}", response.StatusCode);
                return Result<FraudCheckResult>.Failure(ErrorMessages.FraudDetection.Failed);
            }

            var result = await response.Content.ReadFromJsonAsync<FraudCheckResult>();
            
            if (result is null)
            {
                logger.LogWarning("Fraud Service returned invalid response");
                return Result<FraudCheckResult>.Failure(ErrorMessages.FraudDetection.Invalid);
            }

            logger.LogInformation("Fraud check result: {RiskLevel}", result.RiskLevel);
            return Result<FraudCheckResult>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Fraud Detection Service");
            return Result<FraudCheckResult>.Failure(ErrorMessages.FraudDetection.Error);
        }
    }
}
