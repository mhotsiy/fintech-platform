using System.Text.Json;
using Confluent.Kafka;
using FintechPlatform.Domain.Events;
using FintechPlatform.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace FintechPlatform.Api.BackgroundServices;

/// <summary>
/// Background service that consumes withdrawal events from Kafka.
/// Handles withdrawal processing workflow and coordination with external banking systems.
/// </summary>
public class WithdrawalEventConsumer : BackgroundService
{
    private readonly ILogger<WithdrawalEventConsumer> _logger;
    private readonly IConsumer<string, string> _consumer;
    private readonly JsonSerializerOptions _jsonOptions;

    public WithdrawalEventConsumer(IOptions<KafkaSettings> settings, ILogger<WithdrawalEventConsumer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = new ConsumerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            GroupId = $"{settings.Value.GroupId}-withdrawals",
            AutoOffsetReset = settings.Value.AutoOffsetReset == "earliest" 
                ? AutoOffsetReset.Earliest 
                : AutoOffsetReset.Latest,
            EnableAutoCommit = settings.Value.EnableAutoCommit,
            EnableAutoOffsetStore = false,
            SessionTimeoutMs = 45000,
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

        _logger.LogInformation("Withdrawal event consumer initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Withdrawal event consumer starting - subscribing to withdrawal-events topic");
        
        _consumer.Subscribe("withdrawal-events");

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
                    
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal Kafka error - stopping consumer");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error processing withdrawal event");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Withdrawal event consumer stopping due to cancellation");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Withdrawal event consumer stopped");
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var eventType = GetHeaderValue(consumeResult.Message.Headers, "event-type");
        
        _logger.LogInformation(
            "Processing withdrawal event: Type={EventType}, Offset={Offset}, Partition={Partition}",
            eventType, consumeResult.Offset, consumeResult.Partition);

        switch (eventType)
        {
            case "WithdrawalRequested":
                var requestedEvent = JsonSerializer.Deserialize<WithdrawalRequestedEvent>(
                    consumeResult.Message.Value, _jsonOptions);
                if (requestedEvent != null)
                {
                    await HandleWithdrawalRequestedAsync(requestedEvent, cancellationToken);
                }
                break;

            default:
                _logger.LogWarning("Unknown event type: {EventType}", eventType);
                break;
        }
    }

    private async Task HandleWithdrawalRequestedAsync(WithdrawalRequestedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Withdrawal requested: WithdrawalId={WithdrawalId}, MerchantId={MerchantId}, Amount={Amount} {Currency}, BankAccount=****{LastFour}",
            @event.WithdrawalId, @event.MerchantId, 
            @event.AmountInMinorUnits / 100.0m, @event.Currency,
            @event.BankAccountNumber.Length >= 4 ? @event.BankAccountNumber[^4..] : "****");

        // TODO: Implement withdrawal processing workflow:
        // - Validate sufficient balance
        // - Initiate ACH/wire transfer
        // - Call external banking API
        // - Update withdrawal status
        // - Handle failures and retries
        // - Send confirmation notifications
        
        await Task.CompletedTask;
    }

    private string? GetHeaderValue(Headers headers, string key)
    {
        var header = headers.FirstOrDefault(h => h.Key == key);
        return header != null ? System.Text.Encoding.UTF8.GetString(header.GetValueBytes()) : null;
    }
}
