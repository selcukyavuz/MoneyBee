namespace MoneyBee.Transfer.Service.Application.Transfers;

public record CompleteTransferRequest
{
    public string ReceiverNationalId { get; init; } = string.Empty;
}
