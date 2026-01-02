using Confluent.Kafka;
using FintechPlatform.Domain.Events;
using FintechPlatform.Workers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FintechPlatform.Workers.Workers;

/// <summary>
/// Fraud detection worker that automatically reviews and approves/flags payments
/// </summary>
public class FraudDetectionWorker : BackgroundService
{
    private readonly ILogger<FraudDetectionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConsumer<string, string> _consumer;

    // Risk assessment thresholds
    private const long AUTO_APPROVE_THRESHOLD = 100000; // $1000.00 in cents
    private const int MERCHANT_PAYMENT_COUNT_THRESHOLD = 0; // New merchant if < 0 payments (allow all for testing)

    public FraudDetectionWorker(
        ILogger<FraudDetectionWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var kafkaBootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "fintechplatform-fraud-detection",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _logger.LogInformation("Fraud detection worker initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fraud detection worker starting - subscribing to payment-events topic");
        
        _consumer.Subscribe("payment-events");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message == null)
                    {
                        _logger.LogDebug("Received null message, continuing...");
                        continue;
                    }

                    _logger.LogInformation("📨 Received message from Kafka");

                    await ProcessPaymentEventAsync(consumeResult, stoppingToken);

                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka: {Reason}", ex.Error.Reason);
                    
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal Kafka error - stopping fraud detection worker");
                        break;
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in fraud detection worker");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Fraud detection worker stopping due to cancellation");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Fraud detection worker stopped");
        }
    }

    private async Task ProcessPaymentEventAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔎 ProcessPaymentEventAsync started");
        
        var eventType = consumeResult.Message.Headers
            .FirstOrDefault(h => h.Key == "event-type")?.GetValueBytes();
        
        if (eventType == null)
        {
            _logger.LogWarning("Event without event-type header received");
            return;
        }

        var eventTypeString = System.Text.Encoding.UTF8.GetString(eventType);
        _logger.LogInformation("Event type: {EventType}", eventTypeString);

        // Only process PaymentCreated events
        if (eventTypeString != "PaymentCreated")
        {
            _logger.LogInformation("Skipping event type: {EventType}", eventTypeString);
            return;
        }

        _logger.LogInformation("Deserializing PaymentCreatedEvent...");
        var paymentCreatedEvent = JsonSerializer.Deserialize<PaymentCreatedEvent>(
            consumeResult.Message.Value,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (paymentCreatedEvent == null)
        {
            _logger.LogWarning("Failed to deserialize PaymentCreatedEvent");
            return;
        }
        
        _logger.LogInformation("Successfully deserialized payment event for PaymentId: {PaymentId}", paymentCreatedEvent.PaymentId);

        _logger.LogInformation(
            "🔍 Fraud Detection: Analyzing payment {PaymentId} for merchant {MerchantId}, Amount: ${Amount} {Currency}",
            paymentCreatedEvent.PaymentId,
            paymentCreatedEvent.MerchantId,
            paymentCreatedEvent.AmountInMinorUnits / 100.0,
            paymentCreatedEvent.Currency);

        // Perform risk assessment
        var riskAssessment = await AssessRiskAsync(paymentCreatedEvent, cancellationToken);

        if (riskAssessment.IsApproved)
        {
            _logger.LogInformation(
                "✅ Fraud Detection: Payment {PaymentId} APPROVED - {Reason}. Auto-completing payment...",
                paymentCreatedEvent.PaymentId,
                riskAssessment.Reason);

            // Automatically complete the payment
            await AutoCompletePaymentAsync(paymentCreatedEvent.PaymentId, cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "⚠️ Fraud Detection: Payment {PaymentId} FLAGGED for manual review - {Reason}",
                paymentCreatedEvent.PaymentId,
                riskAssessment.Reason);
        }
    }

    private async Task<RiskAssessment> AssessRiskAsync(PaymentCreatedEvent paymentEvent, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var paymentWorkflowService = scope.ServiceProvider.GetRequiredService<PaymentWorkflowService>();

        // Rule 1: Check payment amount
        if (paymentEvent.AmountInMinorUnits >= AUTO_APPROVE_THRESHOLD)
        {
            return new RiskAssessment
            {
                IsApproved = false,
                Reason = $"High value transaction (>= ${AUTO_APPROVE_THRESHOLD / 100.0})"
            };
        }

        // Rule 2: Check merchant history
        var completedPaymentCount = await paymentWorkflowService.GetMerchantCompletedPaymentCountAsync(
            paymentEvent.MerchantId,
            cancellationToken);

        if (completedPaymentCount < MERCHANT_PAYMENT_COUNT_THRESHOLD)
        {
            return new RiskAssessment
            {
                IsApproved = false,
                Reason = $"New merchant (only {completedPaymentCount} completed payments)"
            };
        }

        // Rule 3: All checks passed - auto-approve
        return new RiskAssessment
        {
            IsApproved = true,
            Reason = $"Low risk: Amount ${paymentEvent.AmountInMinorUnits / 100.0}, Merchant has {completedPaymentCount} completed payments"
        };
    }

    private async Task AutoCompletePaymentAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var paymentWorkflowService = scope.ServiceProvider.GetRequiredService<PaymentWorkflowService>();

            await paymentWorkflowService.CompletePaymentAsync(paymentId, cancellationToken);

            _logger.LogInformation(
                "✅ Fraud Detection: Successfully auto-completed payment {PaymentId}",
                paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ Fraud Detection: Failed to auto-complete payment {PaymentId}",
                paymentId);
        }
    }

    private record RiskAssessment
    {
        public required bool IsApproved { get; init; }
        public required string Reason { get; init; }
    }
}
