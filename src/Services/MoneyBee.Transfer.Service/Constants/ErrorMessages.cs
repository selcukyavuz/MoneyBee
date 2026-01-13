namespace MoneyBee.Transfer.Service.Constants;

/// <summary>
/// Error message constants for Transfer Service
/// </summary>
public static class ErrorMessages
{
    public static class Customer
    {
        public const string NotFound = "Customer not found";
    }

    public static class Transfer
    {
        public const string ReceiverVerificationFailed = "Receiver identity verification failed";
        public const string CannotBeCompleted = "Transfer cannot be completed. Status: {0}";
        public const string BlockedCustomer = "Transfer cannot be processed. Customer status: {0}";
        public const string DailyLimitExceeded = "Daily transfer limit exceeded. Remaining: {0:F2} TRY";
    }

    public static class ExchangeRate
    {
        public const string Failed = "Failed to get exchange rate";
        public const string Invalid = "Invalid exchange rate received";
        public const string Unavailable = "Exchange rate service temporarily unavailable";
        public const string Error = "Error getting exchange rate";
    }

    public static class FraudDetection
    {
        public const string Failed = "Failed to check fraud risk";
        public const string Invalid = "Invalid fraud check response";
        public const string Unavailable = "Fraud detection service temporarily unavailable";
        public const string Error = "Error checking fraud";
    }
}
