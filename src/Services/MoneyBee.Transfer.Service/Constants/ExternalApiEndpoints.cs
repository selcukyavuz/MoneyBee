namespace MoneyBee.Transfer.Service.Constants;

/// <summary>
/// External service API endpoints
/// </summary>
public static class ExternalApiEndpoints
{
    /// <summary>
    /// Customer Service API endpoints
    /// </summary>
    public static class CustomerService
    {
        public const string GetByNationalId = "/api/customers/by-national-id/{0}";
    }

    /// <summary>
    /// Fraud Detection Service API endpoints
    /// </summary>
    public static class FraudDetection
    {
        public const string CheckTransfer = "/api/fraud/check";
    }

    /// <summary>
    /// Exchange Rate Service API endpoints
    /// </summary>
    public static class ExchangeRate
    {
        public const string GetRate = "/api/exchange-rate";
    }
}
