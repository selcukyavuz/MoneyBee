using MoneyBee.Common.Events;
using MoneyBee.Common.Results;
using MoneyBee.Common.Abstractions;
using MoneyBee.Transfer.Service.Application.Transfers.Shared;
using MoneyBee.Transfer.Service.Domain.Transfers;

namespace MoneyBee.Transfer.Service.Application.Transfers.Commands.CompleteTransfer;

/// <summary>
/// Handles the completion of pending transfers
/// </summary>
public class CompleteTransferHandler(
    ITransferRepository repository,
    IEventPublisher eventPublisher,
    ILogger<CompleteTransferHandler> logger) : ICommandHandler<string, Result<TransferDto>>
{
    public async Task<Result<TransferDto>> HandleAsync(string transactionCode, CancellationToken cancellationToken = default)
    {
        var transfer = await repository.GetByTransactionCodeAsync(transactionCode);

        if (transfer is null)
        {
            return Result<TransferDto>.NotFound(TransferErrors.TransferNotFound);
        }

        // Validate transfer can be completed
        var validationResult = transfer.ValidateForCompletion();
        if (!validationResult.IsSuccess)
        {
            return Result<TransferDto>.Failure(validationResult.Error!);
        }

        // Use aggregate method to complete
        transfer.Complete();

        await repository.UpdateAsync(transfer);

        // Publish event
        await eventPublisher.PublishAsync(new TransferCompletedEvent
        {
            TransferId = transfer.Id,
            TransactionCode = transfer.TransactionCode
        });

        logger.LogInformation("Transfer completed: {TransferId} - Code: {TransactionCode}",
            transfer.Id, transfer.TransactionCode);

        return Result<TransferDto>.Success(TransferMapper.ToDto(transfer));
    }
}
