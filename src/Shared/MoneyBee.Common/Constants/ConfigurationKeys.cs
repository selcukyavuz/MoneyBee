namespace MoneyBee.Common.Constants;

public static class ConfigurationKeys
{   
    public static class ExternalServices
    {
        public const string SectionName = "ExternalServices";
        public const string CustomerService = "ExternalServices:CustomerService";
        public const string FraudService = "ExternalServices:FraudService";
        public const string ExchangeRateService = "ExternalServices:ExchangeRateService";
        public const string KycService = "ExternalServices:KycService";
    }

    public static class Transfer
    {
        public const string SectionName = "TransferSettings";
    }

    public static class Fee
    {
        public const string SectionName = "FeeSettings";
    }

    public static class Services
    {
        public const string AuthService = "Services:AuthService";
    }

    public static class RabbitMQ
    {
        public const string SectionName = "RabbitMQ";
    }
}
