namespace MoneyBee.Transfer.Service.Infrastructure.Constants;

/// <summary>
/// External API endpoint constants
/// </summary>
public static class ExternalApiEndpoints
{
    public static class CustomerService
    {
        public const string GetByNationalId = "/api/customers/by-national-id/{0}";
    }

    public static class ExchangeRate
    {
        public const string GetRate = "/api/exchange-rates";
    }

    public static class FraudDetection
    {
        public const string CheckTransfer = "/api/fraud/check";
    }
}
