using MoneyBee.Transfer.Service.Application.DTOs;

namespace MoneyBee.Transfer.Service.Application.Interfaces;

public interface ITransferService
{
    Task<CreateTransferResponse> CreateTransferAsync(CreateTransferRequest request);
    Task<TransferDto> CompleteTransferAsync(string transactionCode, CompleteTransferRequest request);
    Task<TransferDto> CancelTransferAsync(string transactionCode, CancelTransferRequest request);
    Task<TransferDto?> GetTransferByCodeAsync(string transactionCode);
    Task<IEnumerable<TransferDto>> GetCustomerTransfersAsync(Guid customerId);
    Task<DailyLimitCheckResponse> CheckDailyLimitAsync(Guid customerId);
}
