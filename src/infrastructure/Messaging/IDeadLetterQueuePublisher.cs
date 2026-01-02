using FintechPlatform.Domain.Events;

namespace FintechPlatform.Infrastructure.Messaging;

/// <summary>
/// Publisher for failed events that couldn't be processed after retries
/// </summary>
public interface IDeadLetterQueuePublisher
{
    /// <summary>
    /// Publish a failed event to the Dead Letter Queue
    /// </summary>
    /// <param name="failedEvent">Details of the failed event</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishToDeadLetterQueueAsync(FailedEventRecord failedEvent, CancellationToken cancellationToken = default);
}
