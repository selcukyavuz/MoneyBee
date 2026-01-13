namespace MoneyBee.Transfer.Service.Application.DTOs;

public record CompleteTransferRequest
{
    public string ReceiverNationalId { get; init; } = string.Empty;
}
