namespace MoneyBee.Transfer.Service.Application.DTOs;

public record CancelTransferRequest
{
    public string Reason { get; init; } = "Customer request";
}
