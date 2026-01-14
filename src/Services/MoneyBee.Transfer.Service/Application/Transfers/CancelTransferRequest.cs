namespace MoneyBee.Transfer.Service.Application.Transfers;

public record CancelTransferRequest
{
    public string Reason { get; init; } = "Customer request";
}
