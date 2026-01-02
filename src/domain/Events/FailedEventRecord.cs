namespace FintechPlatform.Domain.Events;

/// <summary>
/// Represents an event that failed processing and was sent to the Dead Letter Queue
/// </summary>
public class FailedEventRecord
{
    public string OriginalTopic { get; }
    public string EventType { get; }
    public string EventPayload { get; }
    public string FailureReason { get; }
    public string ExceptionDetails { get; }
    public int RetryCount { get; }
    public DateTime FirstFailedAt { get; }
    public DateTime LastFailedAt { get; }
    public string ConsumerGroup { get; }
    public int OriginalPartition { get; }
    public long OriginalOffset { get; }

    public FailedEventRecord(
        string originalTopic,
        string eventType,
        string eventPayload,
        string failureReason,
        string exceptionDetails,
        int retryCount,
        DateTime firstFailedAt,
        DateTime lastFailedAt,
        string consumerGroup,
        int originalPartition,
        long originalOffset)
    {
        OriginalTopic = originalTopic;
        EventType = eventType;
        EventPayload = eventPayload;
        FailureReason = failureReason;
        ExceptionDetails = exceptionDetails;
        RetryCount = retryCount;
        FirstFailedAt = firstFailedAt;
        LastFailedAt = lastFailedAt;
        ConsumerGroup = consumerGroup;
        OriginalPartition = originalPartition;
        OriginalOffset = originalOffset;
    }
}
