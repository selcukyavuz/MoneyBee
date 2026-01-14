namespace MoneyBee.Common.Abstractions;

/// <summary>
/// Base interface for command handlers that modify state (CQRS pattern)
/// </summary>
/// <typeparam name="TRequest">The command request type</typeparam>
/// <typeparam name="TResponse">The command response type</typeparam>
public interface ICommandHandler<TRequest, TResponse>
{
    /// <summary>
    /// Handles the command asynchronously
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The command response</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
