using MoneyBee.Common.Results;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;

namespace MoneyBee.Transfer.Service.Application.Transfers.Services;

/// <summary>
/// Service interface for fraud detection checks on money transfers
/// </summary>
public interface IFraudDetectionService
{
    /// <summary>
    /// Checks a money transfer for fraud risk
    /// </summary>
    /// <param name="senderId">The sender's unique identifier</param>
    /// <param name="receiverId">The receiver's unique identifier</param>
    /// <param name="amountInTRY">The transfer amount in TRY</param>
    /// <param name="senderNationalId">The sender's national ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing fraud check result with risk level and approval status</returns>
    Task<Result<FraudCheckResult>> CheckTransferAsync(Guid senderId, Guid receiverId, decimal amountInTRY, string senderNationalId, CancellationToken cancellationToken = default);
}
