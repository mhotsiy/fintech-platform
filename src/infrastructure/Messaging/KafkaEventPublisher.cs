using System.Text.Json;
using Confluent.Kafka;
using FintechPlatform.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FintechPlatform.Infrastructure.Messaging;

/// <summary>
/// Kafka implementation of the event publisher.
/// Provides reliable, ordered, durable event publishing with at-least-once delivery guarantees.
/// </summary>
public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaEventPublisher(IOptions<KafkaSettings> settings, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            EnableIdempotence = settings.Value.EnableIdempotence,
            Acks = settings.Value.Acks == "all" ? Acks.All : 
                   settings.Value.Acks == "1" ? Acks.Leader : Acks.None,
            MessageSendMaxRetries = settings.Value.MessageSendMaxRetries,
            RetryBackoffMs = 100,
            RequestTimeoutMs = 30000,
            // Ensure ordering within a partition
            MaxInFlight = 5,
            // Compression for efficiency
            CompressionType = CompressionType.Snappy
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka producer error: {Error}", error.Reason);
            })
            .SetLogHandler((_, logMessage) =>
            {
                var logLevel = logMessage.Level switch
                {
                    SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
                    SyslogLevel.Warning => LogLevel.Warning,
                    SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                    _ => LogLevel.Debug
                };
                _logger.Log(logLevel, "Kafka: {Message}", logMessage.Message);
            })
            .Build();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _logger.LogInformation("Kafka producer initialized with bootstrap servers: {BootstrapServers}",
            settings.Value.BootstrapServers);
    }

    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        if (string.IsNullOrWhiteSpace(topic)) throw new ArgumentException("Topic cannot be empty", nameof(topic));

        try
        {
            var key = @event.EventId.ToString();
            var value = JsonSerializer.Serialize(@event, _jsonOptions);

            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = new Headers
                {
                    { "event-type", System.Text.Encoding.UTF8.GetBytes(@event.EventType) },
                    { "occurred-at", System.Text.Encoding.UTF8.GetBytes(@event.OccurredAt.ToString("O")) }
                }
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} with ID {EventId} to topic {Topic} at offset {Offset}",
                @event.EventType, @event.EventId, topic, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventType} with ID {EventId} to topic {Topic}. Error: {Error}",
                @event.EventType, @event.EventId, topic, ex.Error.Reason);
            throw new InvalidOperationException($"Failed to publish event to Kafka: {ex.Error.Reason}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error publishing event {EventType} with ID {EventId} to topic {Topic}",
                @event.EventType, @event.EventId, topic);
            throw;
        }
    }

    public async Task PublishBatchAsync<TEvent>(string topic, IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        if (events == null) throw new ArgumentNullException(nameof(events));

        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            _logger.LogWarning("Attempted to publish empty batch to topic {Topic}", topic);
            return;
        }

        _logger.LogInformation("Publishing batch of {Count} events to topic {Topic}", eventList.Count, topic);

        var tasks = eventList.Select(e => PublishAsync(topic, e, cancellationToken));
        await Task.WhenAll(tasks);

        _logger.LogInformation("Successfully published batch of {Count} events to topic {Topic}", eventList.Count, topic);
    }

    public void Dispose()
    {
        _logger.LogInformation("Disposing Kafka producer - flushing pending messages");
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
