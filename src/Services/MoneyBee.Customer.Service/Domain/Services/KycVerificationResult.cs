namespace MoneyBee.Customer.Service.Domain.Services;

/// <summary>
/// Represents the result of a KYC verification request.
/// </summary>
public class KycVerificationResult
{
    /// <summary>
    /// Indicates whether the customer's identity was verified successfully.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Descriptive message about the verification result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional risk score assigned to the customer (if provided by KYC service).
    /// </summary>
    public string? RiskScore { get; set; }
}
