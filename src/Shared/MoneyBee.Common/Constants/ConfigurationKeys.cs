namespace MoneyBee.Common.Constants;

/// <summary>
/// Configuration section and key names used in appsettings.json
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>
    /// External services configuration section
    /// </summary>
    public static class ExternalServices
    {
        public const string SectionName = "ExternalServices";
        public const string CustomerService = "ExternalServices:CustomerService";
        public const string FraudService = "ExternalServices:FraudService";
        public const string ExchangeRateService = "ExternalServices:ExchangeRateService";
        public const string KycService = "ExternalServices:KycService";
    }

    /// <summary>
    /// Transfer settings configuration section
    /// </summary>
    public static class Transfer
    {
        public const string SectionName = "TransferSettings";
    }

    /// <summary>
    /// Fee settings configuration section
    /// </summary>
    public static class Fee
    {
        public const string SectionName = "FeeSettings";
    }

    /// <summary>
    /// Distributed lock settings configuration section
    /// </summary>
    public static class DistributedLock
    {
        public const string SectionName = "DistributedLockSettings";
    }
}
