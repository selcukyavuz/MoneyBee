namespace MoneyBee.Common.Abstractions;

/// <summary>
/// Base interface for query handlers that retrieve data (CQRS pattern)
/// </summary>
/// <typeparam name="TRequest">The query request type</typeparam>
/// <typeparam name="TResponse">The query response type</typeparam>
public interface IQueryHandler<TRequest, TResponse>
{
    /// <summary>
    /// Handles the query asynchronously
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The query response</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
