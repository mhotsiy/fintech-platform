using System.Text.Json;
using Confluent.Kafka;
using FintechPlatform.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FintechPlatform.Infrastructure.Messaging;

/// <summary>
/// Kafka implementation of Dead Letter Queue publisher
/// Sends failed events to a dedicated DLQ topic for later analysis and reprocessing
/// </summary>
public class KafkaDeadLetterQueuePublisher : IDeadLetterQueuePublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaDeadLetterQueuePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string DLQ_TOPIC = "dead-letter-queue";

    public KafkaDeadLetterQueuePublisher(IOptions<KafkaSettings> settings, ILogger<KafkaDeadLetterQueuePublisher> _logger)
    {
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));

        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100,
            RequestTimeoutMs = 30000
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("DLQ producer error: {Error}", error.Reason);
            })
            .Build();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true  // More readable for manual inspection
        };

        _logger.LogInformation("Dead Letter Queue publisher initialized");
    }

    public async Task PublishToDeadLetterQueueAsync(FailedEventRecord failedEvent, CancellationToken cancellationToken = default)
    {
        if (failedEvent == null)
        {
            throw new ArgumentNullException(nameof(failedEvent));
        }

        try
        {
            var dlqMessage = new
            {
                failedEvent.OriginalTopic,
                failedEvent.EventType,
                failedEvent.EventPayload,
                failedEvent.FailureReason,
                failedEvent.ExceptionDetails,
                failedEvent.RetryCount,
                failedEvent.FirstFailedAt,
                failedEvent.LastFailedAt,
                failedEvent.ConsumerGroup,
                failedEvent.OriginalPartition,
                failedEvent.OriginalOffset,
                SentToDlqAt = DateTime.UtcNow
            };

            var key = $"{failedEvent.OriginalTopic}:{failedEvent.OriginalPartition}:{failedEvent.OriginalOffset}";
            var value = JsonSerializer.Serialize(dlqMessage, _jsonOptions);

            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = new Headers
                {
                    { "original-topic", System.Text.Encoding.UTF8.GetBytes(failedEvent.OriginalTopic) },
                    { "original-event-type", System.Text.Encoding.UTF8.GetBytes(failedEvent.EventType) },
                    { "failure-reason", System.Text.Encoding.UTF8.GetBytes(failedEvent.FailureReason) },
                    { "retry-count", System.Text.Encoding.UTF8.GetBytes(failedEvent.RetryCount.ToString()) },
                    { "consumer-group", System.Text.Encoding.UTF8.GetBytes(failedEvent.ConsumerGroup) },
                    { "dlq-timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                }
            };

            var result = await _producer.ProduceAsync(DLQ_TOPIC, message, cancellationToken);

            _logger.LogWarning(
                "[DLQ] Event sent to DLQ | Topic: {OriginalTopic} | EventType: {EventType} | Reason: {FailureReason} | RetryCount: {RetryCount} | DLQ Offset: {DlqOffset}",
                failedEvent.OriginalTopic,
                failedEvent.EventType,
                failedEvent.FailureReason,
                failedEvent.RetryCount,
                result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogCritical(ex,
                "❌ CRITICAL: Failed to publish to DLQ! Event will be lost. Original topic: {OriginalTopic}, Event type: {EventType}",
                failedEvent.OriginalTopic,
                failedEvent.EventType);
            
            // Don't throw - we don't want to crash the consumer because DLQ failed
            // In production, this would trigger alerts for manual intervention
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "❌ CRITICAL: Unexpected error publishing to DLQ. Original topic: {OriginalTopic}",
                failedEvent.OriginalTopic);
        }
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing DLQ producer - flushing pending messages");
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
