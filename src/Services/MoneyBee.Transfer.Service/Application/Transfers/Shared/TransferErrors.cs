namespace MoneyBee.Transfer.Service.Application.Transfers.Shared;

public static class TransferErrors
{
    // Transfer operations
    public const string IdempotencyKeyRequired = "Idempotency key is required for transfer operations";
    public const string AmountMustBePositive = "Amount must be greater than zero";
    public const string SenderNotFound = "Sender customer not found";
    public const string SenderNotActive = "Sender customer is not active";
    public const string ReceiverNotFound = "Receiver customer not found";
    public const string ReceiverNotActive = "Receiver customer is not active";
    public const string ExchangeRateUnavailable = "Exchange rate service unavailable. Please try again later.";
    public const string HighFraudRisk = "Transfer rejected due to high fraud risk";
    public const string FraudCheckFailed = "Fraud check failed. Please try again later.";
    public const string TransferNotFound = "Transfer not found";
    
    // Customer errors
    public const string CustomerNotFound = "Customer not found";
    public const string CustomerServiceUnavailable = "Customer service is temporarily unavailable";
    
    // Transfer validation
    public const string ReceiverVerificationFailed = "Receiver identity verification failed";
    public const string CannotBeCompleted = "Transfer cannot be completed. Status: {0}";
    public const string DailyLimitExceeded = "Daily transfer limit exceeded. Remaining: {0:F2} TRY";
    
    // Exchange rate errors
    public const string ExchangeRateFailed = "Failed to get exchange rate";
    public const string ExchangeRateInvalid = "Invalid exchange rate received";
    public const string ExchangeRateError = "Error getting exchange rate";
    
    // Fraud detection errors
    public const string FraudDetectionFailed = "Failed to check fraud risk";
    public const string FraudDetectionInvalid = "Invalid fraud check response";
    public const string FraudDetectionError = "Error checking fraud";
}
