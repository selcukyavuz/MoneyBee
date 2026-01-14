namespace MoneyBee.Common.Abstractions;

/// <summary>
/// Interface for publishing domain events to message broker
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the message broker
    /// </summary>
    /// <typeparam name="T">The event type</typeparam>
    /// <param name="eventData">The event data to publish</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task PublishAsync<T>(T eventData) where T : class;
}
