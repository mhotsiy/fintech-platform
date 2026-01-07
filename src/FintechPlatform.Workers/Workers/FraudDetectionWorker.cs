using Confluent.Kafka;
using FintechPlatform.Domain.Events;
using FintechPlatform.Infrastructure.Messaging;
using FintechPlatform.Workers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
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
    private readonly IDeadLetterQueuePublisher _dlqPublisher;

    // Risk assessment thresholds
    private const long AUTO_APPROVE_THRESHOLD = 100000; // $1000.00 in cents
    private const int MERCHANT_PAYMENT_COUNT_THRESHOLD = 0; // New merchant if < 0 payments (allow all for testing)
    
    // Retry configuration
    private const int MAX_RETRY_ATTEMPTS = 3;
    private readonly Dictionary<string, RetryInfo> _retryTracking = new();
    
    // Prometheus Metrics
    private static readonly Counter PaymentsProcessedTotal = Metrics
        .CreateCounter("payments_processed_total", "Total number of payment events processed",
            new CounterConfiguration { LabelNames = new[] { "status" } });
    
    private static readonly Counter PaymentsApprovedTotal = Metrics
        .CreateCounter("payments_approved_total", "Total number of payments approved by fraud detection");
    
    private static readonly Counter PaymentsFlaggedTotal = Metrics
        .CreateCounter("payments_flagged_total", "Total number of payments flagged for manual review",
            new CounterConfiguration { LabelNames = new[] { "reason" } });
    
    private static readonly Counter PaymentsAutoCompletedTotal = Metrics
        .CreateCounter("payments_auto_completed_total", "Total number of payments auto-completed");
    
    private static readonly Counter DlqMessagesTotal = Metrics
        .CreateCounter("dlq_messages_total", "Total messages sent to Dead Letter Queue",
            new CounterConfiguration { LabelNames = new[] { "failure_reason" } });
    
    private static readonly Counter RetryAttemptsTotal = Metrics
        .CreateCounter("retry_attempts_total", "Total retry attempts for failed messages");
    
    private static readonly Histogram ProcessingDuration = Metrics
        .CreateHistogram("payment_processing_duration_seconds", "Time spent processing each payment event",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10) // 0.1s to 1s
            });
    
    private static readonly Gauge KafkaConsumerLag = Metrics
        .CreateGauge("kafka_consumer_lag", "Current Kafka consumer lag (messages behind)",
            new GaugeConfiguration { LabelNames = new[] { "topic", "partition" } });
    
    // Business metrics
    private static readonly Counter PaymentAmountProcessedTotal = Metrics
        .CreateCounter("payment_amount_processed_total", "Total payment amount processed in minor units (cents)",
            new CounterConfiguration { LabelNames = new[] { "currency", "status" } });
    
    private static readonly Histogram PaymentAmountDistribution = Metrics
        .CreateHistogram("payment_amount_distribution", "Distribution of payment amounts in minor units",
            new HistogramConfiguration
            {
                LabelNames = new[] { "currency" },
                Buckets = new[] { 1000.0, 5000.0, 10000.0, 50000.0, 100000.0, 500000.0, 1000000.0 }
            });
    
    private static readonly Counter PaymentsByCurrency = Metrics
        .CreateCounter("payments_by_currency_total", "Total payments grouped by currency",
            new CounterConfiguration { LabelNames = new[] { "currency" } });
    
    private static readonly Gauge ActivePaymentsProcessing = Metrics
        .CreateGauge("active_payments_processing", "Number of payments currently being processed");

    public FraudDetectionWorker(
        ILogger<FraudDetectionWorker> logger,
        IServiceProvider serviceProvider,
        IDeadLetterQueuePublisher dlqPublisher,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dlqPublisher = dlqPublisher;

        var kafkaBootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "fintechplatform-fraud-detection",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _logger.LogInformation("Fraud detection worker initialized with DLQ support");
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

                    var success = await ProcessPaymentEventAsync(consumeResult, stoppingToken);

                    if (success)
                    {
                        // Only commit offset if processing succeeded
                        _consumer.Commit(consumeResult);
                    }
                    else
                    {
                        // Processing failed after retries - already sent to DLQ
                        // Commit offset to move past this message
                        _consumer.Commit(consumeResult);
                        _logger.LogWarning("Message processing failed and sent to DLQ, offset committed to continue");
                    }
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

    private async Task<bool> ProcessPaymentEventAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var messageKey = $"{consumeResult.TopicPartition}:{consumeResult.Offset}";
        
        // Start timing and track active processing
        using var timer = ProcessingDuration.NewTimer();
        ActivePaymentsProcessing.Inc();
        
        try
        {
            _logger.LogInformation("🔎 ProcessPaymentEventAsync started");
            
            var eventType = consumeResult.Message.Headers
                .FirstOrDefault(h => h.Key == "event-type")?.GetValueBytes();
            
            if (eventType == null)
            {
                _logger.LogWarning("Event without event-type header received");
                await SendToDlqAsync(consumeResult, "MissingEventTypeHeader", "Event does not have event-type header", 0, cancellationToken);
                PaymentsProcessedTotal.WithLabels("missing_header").Inc();
                return false;
            }

            var eventTypeString = System.Text.Encoding.UTF8.GetString(eventType);
            _logger.LogInformation("Event type: {EventType}", eventTypeString);

            // Only process PaymentCreated events
            if (eventTypeString != "PaymentCreated")
            {
                _logger.LogInformation("Skipping event type: {EventType}", eventTypeString);
                PaymentsProcessedTotal.WithLabels("skipped").Inc();
                return true; // Not an error, just not our event
            }

            _logger.LogInformation("Deserializing PaymentCreatedEvent...");
            var paymentCreatedEvent = JsonSerializer.Deserialize<PaymentCreatedEvent>(
                consumeResult.Message.Value,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paymentCreatedEvent == null)
            {
                _logger.LogWarning("Failed to deserialize PaymentCreatedEvent");
                await SendToDlqAsync(consumeResult, "DeserializationFailure", "Failed to deserialize PaymentCreatedEvent from JSON", 0, cancellationToken);
                PaymentsProcessedTotal.WithLabels("deserialization_error").Inc();
                return false;
            }
            
            _logger.LogInformation("Successfully deserialized payment event for PaymentId: {PaymentId}", paymentCreatedEvent.PaymentId);

            // Track business metrics
            PaymentsByCurrency.WithLabels(paymentCreatedEvent.Currency).Inc();
            PaymentAmountDistribution.WithLabels(paymentCreatedEvent.Currency).Observe(paymentCreatedEvent.AmountInMinorUnits);

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
                PaymentsApprovedTotal.Inc();
                PaymentAmountProcessedTotal.WithLabels(paymentCreatedEvent.Currency, "approved").Inc(paymentCreatedEvent.AmountInMinorUnits);
                
                _logger.LogInformation(
                    "✅ Fraud Detection: Payment {PaymentId} APPROVED - {Reason}. Auto-completing payment...",
                    paymentCreatedEvent.PaymentId,
                    riskAssessment.Reason);

                // Automatically complete the payment
                await AutoCompletePaymentAsync(paymentCreatedEvent.PaymentId, cancellationToken);
                PaymentsAutoCompletedTotal.Inc();
            }
            else
            {
                PaymentsFlaggedTotal.WithLabels(riskAssessment.Reason).Inc();
                PaymentAmountProcessedTotal.WithLabels(paymentCreatedEvent.Currency, "flagged").Inc(paymentCreatedEvent.AmountInMinorUnits);
                
                _logger.LogWarning(
                    "⚠️ Fraud Detection: Payment {PaymentId} FLAGGED for manual review - {Reason}",
                    paymentCreatedEvent.PaymentId,
                    riskAssessment.Reason);

                // Publish PaymentFlaggedEvent for SignalR notification
                await PublishPaymentFlaggedEventAsync(paymentCreatedEvent, riskAssessment.Reason, cancellationToken);
            }

            // Success - clear retry tracking
            _retryTracking.Remove(messageKey);
            PaymentsProcessedTotal.WithLabels("success").Inc();
            return true;
        }
        catch (JsonException ex)
        {
            // JSON errors are permanent - send to DLQ immediately
            _logger.LogError(ex, "JSON deserialization error (permanent failure)");
            await SendToDlqAsync(consumeResult, "JsonException", ex.Message, 0, cancellationToken);
            PaymentsProcessedTotal.WithLabels("json_error").Inc();
            return false;
        }
        catch (InvalidOperationException ex)
        {
            // Business logic errors - might be transient (e.g., balance not found yet)
            return await HandleRetryableErrorAsync(consumeResult, messageKey, "InvalidOperationException", ex, cancellationToken);
        }
        catch (Exception ex)
        {
            // Unknown errors - retry with caution
            return await HandleRetryableErrorAsync(consumeResult, messageKey, "UnexpectedException", ex, cancellationToken);
        }
        finally
        {
            // Always decrement active processing gauge
            ActivePaymentsProcessing.Dec();
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

            await paymentWorkflowService.CompletePaymentAsync(paymentId, Domain.Entities.CompletionSource.FraudDetection, cancellationToken);

            _logger.LogInformation(
                "[SUCCESS] Fraud Detection: Successfully auto-completed payment {PaymentId}",
                paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[FAILED] Fraud Detection: Failed to auto-complete payment {PaymentId}",
                paymentId);
        }
    }

    private async Task PublishPaymentFlaggedEventAsync(
        PaymentCreatedEvent paymentCreatedEvent,
        string flagReason,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var flaggedEvent = new PaymentFlaggedEvent(
                paymentCreatedEvent.PaymentId,
                paymentCreatedEvent.MerchantId,
                paymentCreatedEvent.AmountInMinorUnits,
                paymentCreatedEvent.Currency,
                flagReason,
                paymentCreatedEvent.ExternalReference,
                paymentCreatedEvent.Description);

            await eventPublisher.PublishAsync("payment-events", flaggedEvent, cancellationToken);

            _logger.LogInformation(
                "📨 Published PaymentFlaggedEvent for payment {PaymentId}",
                paymentCreatedEvent.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to publish PaymentFlaggedEvent for payment {PaymentId}",
                paymentCreatedEvent.PaymentId);
        }
    }

    private async Task<bool> HandleRetryableErrorAsync(
        ConsumeResult<string, string> consumeResult,
        string messageKey,
        string errorType,
        Exception ex,
        CancellationToken cancellationToken)
    {
        // Track retry attempts
        if (!_retryTracking.ContainsKey(messageKey))
        {
            _retryTracking[messageKey] = new RetryInfo
            {
                FirstAttemptTime = DateTime.UtcNow,
                RetryCount = 0
            };
        }

        var retryInfo = _retryTracking[messageKey];
        retryInfo.RetryCount++;
        retryInfo.LastAttemptTime = DateTime.UtcNow;
        
        RetryAttemptsTotal.Inc();

        if (retryInfo.RetryCount <= MAX_RETRY_ATTEMPTS)
        {
            var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, retryInfo.RetryCount)); // Exponential backoff: 2s, 4s, 8s
            
            _logger.LogWarning(ex,
                "🔄 Retryable error (attempt {RetryCount}/{MaxRetries}). Waiting {BackoffSeconds}s before retry. Error: {ErrorType}",
                retryInfo.RetryCount,
                MAX_RETRY_ATTEMPTS,
                backoffDelay.TotalSeconds,
                errorType);

            await Task.Delay(backoffDelay, cancellationToken);
            PaymentsProcessedTotal.WithLabels("retrying").Inc();
            return false; // Will retry on next poll
        }
        else
        {
            // Max retries exceeded - send to DLQ
            _logger.LogError(ex,
                "❌ Max retries ({MaxRetries}) exceeded for message. Sending to DLQ. Error: {ErrorType}",
                MAX_RETRY_ATTEMPTS,
                errorType);

            await SendToDlqAsync(consumeResult, errorType, ex.Message + "\n\n" + ex.StackTrace, retryInfo.RetryCount, cancellationToken);
            _retryTracking.Remove(messageKey);
            PaymentsProcessedTotal.WithLabels("max_retries_exceeded").Inc();
            return false; // Sent to DLQ
        }
    }

    private async Task SendToDlqAsync(
        ConsumeResult<string, string> consumeResult,
        string failureReason,
        string? exceptionDetails,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var eventType = consumeResult.Message.Headers
            .FirstOrDefault(h => h.Key == "event-type")?.GetValueBytes();
        var eventTypeString = eventType != null ? System.Text.Encoding.UTF8.GetString(eventType) : "Unknown";

        var failedEvent = new FailedEventRecord(
            originalTopic: consumeResult.Topic,
            eventType: eventTypeString,
            eventPayload: consumeResult.Message.Value,
            failureReason: failureReason,
            exceptionDetails: exceptionDetails ?? "No details available",
            retryCount: retryCount,
            firstFailedAt: DateTime.UtcNow,
            lastFailedAt: DateTime.UtcNow,
            consumerGroup: "fintechplatform-fraud-detection",
            originalPartition: consumeResult.Partition.Value,
            originalOffset: consumeResult.Offset.Value
        );

        await _dlqPublisher.PublishToDeadLetterQueueAsync(failedEvent, cancellationToken);
        DlqMessagesTotal.WithLabels(failureReason).Inc();
    }

    private record RiskAssessment
    {
        public required bool IsApproved { get; init; }
        public required string Reason { get; init; }
    }

    private class RetryInfo
    {
        public DateTime FirstAttemptTime { get; set; }
        public DateTime LastAttemptTime { get; set; }
        public int RetryCount { get; set; }
    }
}
