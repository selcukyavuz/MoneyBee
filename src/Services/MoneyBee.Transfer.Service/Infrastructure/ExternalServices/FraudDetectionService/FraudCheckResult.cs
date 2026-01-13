using MoneyBee.Common.Enums;

namespace MoneyBee.Transfer.Service.Infrastructure.ExternalServices.FraudDetectionService;

/// <summary>
/// Represents the result of a fraud detection check
/// </summary>
public record FraudCheckResult
{
    public required RiskLevel RiskLevel { get; init; }
    public bool IsApproved => RiskLevel == RiskLevel.Low;
    public required string Message { get; init; }
}
