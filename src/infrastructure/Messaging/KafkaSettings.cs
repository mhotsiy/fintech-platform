namespace FintechPlatform.Infrastructure.Messaging;

/// <summary>
/// Configuration settings for Kafka connectivity
/// </summary>
public class KafkaSettings
{
    public const string SectionName = "Kafka";

    /// <summary>
    /// Comma-separated list of Kafka broker addresses (e.g., "localhost:9092")
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Consumer group ID for event consumers
    /// </summary>
    public string GroupId { get; set; } = "fintechplatform-api";

    /// <summary>
    /// Enable idempotent producer for exactly-once semantics
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// Number of acknowledgments the producer requires (all = -1, leader = 1, none = 0)
    /// </summary>
    public string Acks { get; set; } = "all";

    /// <summary>
    /// Maximum number of retries for failed produce requests
    /// </summary>
    public int MessageSendMaxRetries { get; set; } = 3;

    /// <summary>
    /// Auto-commit consumer offsets
    /// </summary>
    public bool EnableAutoCommit { get; set; } = false;

    /// <summary>
    /// Where to start consuming if no committed offset exists (earliest, latest)
    /// </summary>
    public string AutoOffsetReset { get; set; } = "earliest";
}
