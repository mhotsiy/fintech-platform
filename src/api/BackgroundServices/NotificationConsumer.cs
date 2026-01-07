using Confluent.Kafka;
using FintechPlatform.Api.Hubs;
using FintechPlatform.Api.Models;
using FintechPlatform.Domain.Events;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace FintechPlatform.Api.BackgroundServices;

/// <summary>
/// Background service that consumes Kafka events and broadcasts real-time notifications via SignalR
/// </summary>
public class NotificationConsumer : BackgroundService
{
    private readonly ILogger<NotificationConsumer> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IConsumer<string, string> _consumer;
    private readonly JsonSerializerOptions _jsonOptions;

    public NotificationConsumer(
        ILogger<NotificationConsumer> logger,
        IHubContext<NotificationHub> hubContext,
        IConfiguration configuration)
    {
        _logger = logger;
        _hubContext = hubContext;

        var kafkaBootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "fintechplatform-notifications",
            AutoOffsetReset = AutoOffsetReset.Latest, // Only new events
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        
        // Use camelCase to match the publisher
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit to ensure the web server starts first
        await Task.Delay(2000, stoppingToken);
        
        _logger.LogInformation("NotificationConsumer starting - subscribing to payment-events");
        _consumer.Subscribe(new[] { "payment-events" });

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result?.Message != null)
                    {
                        var eventType = GetEventType(result.Message);
                        _logger.LogDebug("Processing event: {EventType}", eventType);

                        await HandleEventAsync(eventType, result.Message.Value);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing notification event");
                }
            }
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("NotificationConsumer stopped");
        }
    }

    private async Task HandleEventAsync(string eventType, string messageValue)
    {
        NotificationMessage? notification = eventType switch
        {
            "PaymentCompleted" => await HandlePaymentCompletedAsync(messageValue),
            "PaymentFlagged" => await HandlePaymentFlaggedAsync(messageValue),
            "WithdrawalCancelled" => await HandleWithdrawalCancelledAsync(messageValue),
            _ => null
        };

        if (notification != null)
        {
            await _hubContext.Clients.All.SendAsync("notification", notification);
            _logger.LogInformation("Broadcast notification: {Type} - {Title}", notification.Type, notification.Title);
        }
    }

    private Task<NotificationMessage?> HandlePaymentCompletedAsync(string messageValue)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentCompletedEvent>(messageValue, _jsonOptions);
            if (@event == null) return Task.FromResult<NotificationMessage?>(null);

            _logger.LogInformation("PaymentCompletedEvent: Amount={Amount}, Balance={Balance}, Currency={Currency}", 
                @event.AmountInMinorUnits, @event.NewBalanceInMinorUnits, @event.Currency);

            var amountFormatted = FormatAmount(@event.AmountInMinorUnits, @event.Currency);
            var balanceFormatted = FormatAmount(@event.NewBalanceInMinorUnits, @event.Currency);

            _logger.LogInformation("Formatted: Amount={AmountFormatted}, Balance={BalanceFormatted}", 
                amountFormatted, balanceFormatted);

            return Task.FromResult<NotificationMessage?>(new NotificationMessage
            {
                Type = "PaymentCompleted",
                Title = "Payment Approved",
                Message = $"Payment of {amountFormatted} has been completed. New balance: {balanceFormatted}",
                Severity = "success",
                Data = new Dictionary<string, object>
                {
                    ["paymentId"] = @event.PaymentId,
                    ["merchantId"] = @event.MerchantId,
                    ["amount"] = @event.AmountInMinorUnits,
                    ["currency"] = @event.Currency,
                    ["newBalance"] = @event.NewBalanceInMinorUnits
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing PaymentCompletedEvent");
            return Task.FromResult<NotificationMessage?>(null);
        }
    }

    private Task<NotificationMessage?> HandlePaymentFlaggedAsync(string messageValue)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<PaymentFlaggedEvent>(messageValue, _jsonOptions);
            if (@event == null) return Task.FromResult<NotificationMessage?>(null);

            var amountFormatted = FormatAmount(@event.AmountInMinorUnits, @event.Currency);

            return Task.FromResult<NotificationMessage?>(new NotificationMessage
            {
                Type = "PaymentFlagged",
                Title = "Payment Requires Manual Approval",
                Message = $"Payment of {amountFormatted} flagged for review: {@event.FlagReason}",
                Severity = "warning",
                Data = new Dictionary<string, object>
                {
                    ["paymentId"] = @event.PaymentId,
                    ["merchantId"] = @event.MerchantId,
                    ["amount"] = @event.AmountInMinorUnits,
                    ["currency"] = @event.Currency,
                    ["flagReason"] = @event.FlagReason
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing PaymentFlaggedEvent");
            return Task.FromResult<NotificationMessage?>(null);
        }
    }

    private Task<NotificationMessage?> HandleWithdrawalCancelledAsync(string messageValue)
    {
        try
        {
            var @event = JsonSerializer.Deserialize<WithdrawalCancelledEvent>(messageValue, _jsonOptions);
            if (@event == null) return Task.FromResult<NotificationMessage?>(null);

            var amountFormatted = FormatAmount(@event.AmountInMinorUnits, @event.Currency);

            return Task.FromResult<NotificationMessage?>(new NotificationMessage
            {
                Type = "WithdrawalCancelled",
                Title = "Withdrawal Cancelled",
                Message = $"Withdrawal of {amountFormatted} was cancelled",
                Severity = "warning",
                Data = new Dictionary<string, object>
                {
                    ["withdrawalId"] = @event.WithdrawalId,
                    ["merchantId"] = @event.MerchantId,
                    ["amount"] = @event.AmountInMinorUnits,
                    ["currency"] = @event.Currency
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing WithdrawalCancelledEvent");
            return Task.FromResult<NotificationMessage?>(null);
        }
    }

    private string GetEventType(Message<string, string> message)
    {
        var eventTypeHeader = message.Headers.FirstOrDefault(h => h.Key == "event-type");
        if (eventTypeHeader == null) return "Unknown";
        
        return System.Text.Encoding.UTF8.GetString(eventTypeHeader.GetValueBytes());
    }

    private string FormatAmount(long amountInMinorUnits, string currency)
    {
        var amount = amountInMinorUnits / 100.0m;
        return $"{currency} {amount:N2}";
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
