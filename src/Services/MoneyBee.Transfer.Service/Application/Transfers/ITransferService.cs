using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Application.Transfers;

namespace MoneyBee.Transfer.Service.Application.Transfers;

/// <summary>
/// Service interface for money transfer operations with fraud detection and daily limit control
/// </summary>
public interface ITransferService
{
    /// <summary>
    /// Creates a new money transfer with fraud check and daily limit validation
    /// </summary>
    /// <param name="request">The transfer creation request</param>
    /// <returns>Result containing the created transfer response with transaction code</returns>
    Task<Result<CreateTransferResponse>> CreateTransferAsync(CreateTransferRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Completes a pending transfer by verifying transaction code and receiver identity
    /// </summary>
    /// <param name="transactionCode">The 8-digit transaction code</param>
    /// <param name="request">The completion request with receiver information</param>
    /// <returns>Result containing the completed transfer DTO</returns>
    Task<Result<TransferDto>> CompleteTransferAsync(string transactionCode, CompleteTransferRequest request);
    
    /// <summary>
    /// Cancels a pending transfer and refunds the fee
    /// </summary>
    /// <param name="transactionCode">The 8-digit transaction code</param>
    /// <param name="request">The cancellation request</param>
    /// <returns>Result containing the cancelled transfer DTO</returns>
    Task<Result<TransferDto>> CancelTransferAsync(string transactionCode, CancelTransferRequest request);
    
    /// <summary>
    /// Gets a transfer by its transaction code
    /// </summary>
    /// <param name="transactionCode">The 8-digit transaction code</param>
    /// <returns>Result containing the transfer DTO if found</returns>
    Task<Result<TransferDto>> GetTransferByCodeAsync(string transactionCode);
    
    /// <summary>
    /// Gets all transfers for a specific customer
    /// </summary>
    /// <param name="customerId">The customer's unique identifier</param>
    /// <returns>Collection of transfer DTOs</returns>
    Task<IEnumerable<TransferDto>> GetCustomerTransfersAsync(Guid customerId);
    
    /// <summary>
    /// Checks the daily transfer limit for a customer
    /// </summary>
    /// <param name="customerId">The customer's unique identifier</param>
    /// <returns>Daily limit check response with used and remaining amounts</returns>
    Task<DailyLimitCheckResponse> CheckDailyLimitAsync(Guid customerId);
}
