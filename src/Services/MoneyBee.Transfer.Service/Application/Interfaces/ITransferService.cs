using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Application.DTOs;

namespace MoneyBee.Transfer.Service.Application.Interfaces;

public interface ITransferService
{
    Task<Result<CreateTransferResponse>> CreateTransferAsync(CreateTransferRequest request);
    Task<Result<TransferDto>> CompleteTransferAsync(string transactionCode, CompleteTransferRequest request);
    Task<Result<TransferDto>> CancelTransferAsync(string transactionCode, CancelTransferRequest request);
    Task<Result<TransferDto>> GetTransferByCodeAsync(string transactionCode);
    Task<IEnumerable<TransferDto>> GetCustomerTransfersAsync(Guid customerId);
    Task<DailyLimitCheckResponse> CheckDailyLimitAsync(Guid customerId);
}
