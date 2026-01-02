using FintechPlatform.Domain.Events;

namespace FintechPlatform.Infrastructure.Messaging;

/// <summary>
/// Abstraction for publishing domain events to a message broker.
/// Implementations should handle serialization, delivery guarantees, and error handling.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single event to the specified topic
    /// </summary>
    /// <param name="topic">The topic/stream to publish to</param>
    /// <param name="event">The event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    /// <summary>
    /// Publishes multiple events in a batch for efficiency
    /// </summary>
    /// <param name="topic">The topic/stream to publish to</param>
    /// <param name="events">The events to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishBatchAsync<TEvent>(string topic, IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
