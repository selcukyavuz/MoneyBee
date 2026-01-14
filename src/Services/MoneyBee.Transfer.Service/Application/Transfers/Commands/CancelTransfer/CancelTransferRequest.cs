namespace MoneyBee.Transfer.Service.Application.Transfers.Commands.CancelTransfer;

public record CancelTransferRequest
{
    public string Reason { get; init; } = "Customer request";
}
