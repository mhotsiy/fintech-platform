using System.Text.Json;
using Confluent.Kafka;
using FintechPlatform.Domain.Events;
using FintechPlatform.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace FintechPlatform.Api.BackgroundServices;

/// <summary>
/// Background service that consumes payment events from Kafka.
/// Demonstrates event-driven architecture for downstream processing (analytics, notifications, etc.)
/// </summary>
public class PaymentEventConsumer : BackgroundService
{
    private readonly ILogger<PaymentEventConsumer> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly JsonSerializerOptions _jsonOptions;

    public PaymentEventConsumer(IOptions<KafkaSettings> settings, ILogger<PaymentEventConsumer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = new ConsumerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            GroupId = settings.Value.GroupId,
            AutoOffsetReset = settings.Value.AutoOffsetReset == "earliest" 
                ? AutoOffsetReset.Earliest 
                : AutoOffsetReset.Latest,
            EnableAutoCommit = settings.Value.EnableAutoCommit,
            EnableAutoOffsetStore = false,
            // Session timeout for consumer group management
            SessionTimeoutMs = 45000,
            // Max time between polls before consumer is considered dead
            MaxPollIntervalMs = 300000
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka consumer error: {Error}", error.Reason);
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
                _logger.Log(logLevel, "Kafka consumer: {Message}", logMessage.Message);
            })
            .Build();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _logger.LogInformation("Payment event consumer initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment event consumer starting - subscribing to payment-events topic");
        
        _consumer.Subscribe("payment-events");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message == null)
                        continue;

                    await ProcessMessageAsync(consumeResult, stoppingToken);

                    // Manually commit offset after successful processing
                    _consumer.StoreOffset(consumeResult);
                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka: {Reason}", ex.Error.Reason);
                    
                    // Don't commit offset on error - message will be reprocessed
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal Kafka error - stopping consumer");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing payment event");
                    
                    // Add delay before retrying to avoid tight loop on persistent errors
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Payment event consumer stopping due to cancellation");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Payment event consumer stopped");
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var eventType = GetHeaderValue(consumeResult.Message.Headers, "event-type");
        
        _logger.LogInformation(
            "Processing payment event: Type={EventType}, Offset={Offset}, Partition={Partition}",
            eventType, consumeResult.Offset, consumeResult.Partition);

        switch (eventType)
        {
            case "PaymentCreated":
                var createdEvent = JsonSerializer.Deserialize<PaymentCreatedEvent>(
                    consumeResult.Message.Value, _jsonOptions);
                if (createdEvent != null)
                {
                    await HandlePaymentCreatedAsync(createdEvent, cancellationToken);
                }
                break;

            case "PaymentCompleted":
                var completedEvent = JsonSerializer.Deserialize<PaymentCompletedEvent>(
                    consumeResult.Message.Value, _jsonOptions);
                if (completedEvent != null)
                {
                    await HandlePaymentCompletedAsync(completedEvent, cancellationToken);
                }
                break;

            default:
                _logger.LogWarning("Unknown event type: {EventType}", eventType);
                break;
        }
    }

    private async Task HandlePaymentCreatedAsync(PaymentCreatedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Payment created: PaymentId={PaymentId}, MerchantId={MerchantId}, Amount={Amount} {Currency}",
            @event.PaymentId, @event.MerchantId, @event.AmountInMinorUnits / 100.0m, @event.Currency);

        // TODO: Implement downstream processing:
        // - Send to analytics system
        // - Trigger fraud detection
        // - Update merchant dashboard
        // - Send webhook notifications
        
        await Task.CompletedTask;
    }

    private async Task HandlePaymentCompletedAsync(PaymentCompletedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Payment completed: PaymentId={PaymentId}, MerchantId={MerchantId}, Amount={Amount} {Currency}, NewBalance={NewBalance}",
            @event.PaymentId, @event.MerchantId, 
            @event.AmountInMinorUnits / 100.0m, @event.Currency,
            @event.NewBalanceInMinorUnits / 100.0m);

        // TODO: Implement downstream processing:
        // - Update data warehouse
        // - Trigger invoice generation
        // - Send payment confirmation email
        // - Update merchant reporting
        // - Reconciliation checks
        
        await Task.CompletedTask;
    }

    private string? GetHeaderValue(Headers headers, string key)
    {
        var header = headers.FirstOrDefault(h => h.Key == key);
        return header != null ? System.Text.Encoding.UTF8.GetString(header.GetValueBytes()) : null;
    }
}
