namespace MoneyBee.Transfer.Service.Application.Transfers.Commands.CompleteTransfer;

public record CompleteTransferRequest
{
    public string ReceiverNationalId { get; init; } = string.Empty;
}
