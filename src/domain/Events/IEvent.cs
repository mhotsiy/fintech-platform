namespace FintechPlatform.Domain.Events;

/// <summary>
/// Base interface for all domain events in the system.
/// Events represent significant business occurrences that have already happened.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for this event instance
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// UTC timestamp when the event occurred
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// The type of event (used for routing and deserialization)
    /// </summary>
    string EventType { get; }
}
