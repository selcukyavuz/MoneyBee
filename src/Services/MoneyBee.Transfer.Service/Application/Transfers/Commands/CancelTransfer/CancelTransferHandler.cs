using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Common.Abstractions;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Domain.Transfers;

namespace MoneyBee.Transfer.Service.Application.Transfers.Commands.CancelTransfer;

/// <summary>
/// Handles the cancellation of pending transfers
/// </summary>
public class CancelTransferHandler(
    ITransferRepository repository,
    IEventPublisher eventPublisher,
    ILogger<CancelTransferHandler> logger) : ICommandHandler<(string TransactionCode, CancelTransferRequest Request), Result<TransferDto>>
{
    public async Task<Result<TransferDto>> HandleAsync((string TransactionCode, CancelTransferRequest Request) request, CancellationToken cancellationToken = default)
    {
        var transfer = await repository.GetByTransactionCodeAsync(request.TransactionCode);

        if (transfer is null)
        {
            return Result<TransferDto>.Failure(TransferErrors.TransferNotFound);
        }

        // Use aggregate method to cancel
        transfer.Cancel(request.Request.Reason);

        await repository.UpdateAsync(transfer);

        // Publish event
        await eventPublisher.PublishAsync(new TransferCancelledEvent
        {
            TransferId = transfer.Id,
            Reason = request.Request.Reason ?? string.Empty
        });

        logger.LogInformation("Transfer cancelled: {TransferId} - Reason: {Reason}",
            transfer.Id, request.Request.Reason);

        return Result<TransferDto>.Success(TransferMapper.ToDto(transfer));
    }
}
