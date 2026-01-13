namespace MoneyBee.Transfer.Service.Application.Constants;

public static class TransferErrors
{
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
}
