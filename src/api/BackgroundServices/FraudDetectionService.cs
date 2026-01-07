using Confluent.Kafka;
using FintechPlatform.Api.Services;
using FintechPlatform.Domain.Events;
using System.Text.Json;

namespace FintechPlatform.Api.BackgroundServices;

/// <summary>
/// Fraud detection service that automatically reviews and approves/flags payments
/// </summary>
public class FraudDetectionService : BackgroundService
{
    private readonly ILogger<FraudDetectionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _kafkaBootstrapServers;

    // Risk assessment thresholds
    private const long AUTO_APPROVE_THRESHOLD = 100000; // $1000.00 in cents
    private const int MERCHANT_PAYMENT_COUNT_THRESHOLD = 5; // New merchant if < 5 payments

    public FraudDetectionService(
        ILogger<FraudDetectionService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _kafkaBootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "fintechplatform-fraud-detection",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _logger.LogInformation("Fraud detection service initialized");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fraud detection service starting - subscribing to payment-events topic");
        
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

                    await ProcessPaymentEventAsync(consumeResult, stoppingToken);

                    _consumer.StoreOffset(consumeResult);
                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka: {Reason}", ex.Error.Reason);
                    
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal Kafka error - stopping fraud detection service");
                        break;
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in fraud detection service");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Fraud detection service stopping due to cancellation");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Fraud detection service stopped");
        }
    }

    private async Task ProcessPaymentEventAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        var eventType = consumeResult.Message.Headers
            .FirstOrDefault(h => h.Key == "event-type")?.GetValueBytes();
        
        if (eventType == null)
        {
            _logger.LogWarning("Event without event-type header received");
            return;
        }

        var eventTypeName = System.Text.Encoding.UTF8.GetString(eventType);

        // Only process PaymentCreatedEvent
        if (eventTypeName != nameof(PaymentCreatedEvent))
        {
            return;
        }

        var paymentCreatedEvent = JsonSerializer.Deserialize<PaymentCreatedEvent>(
            consumeResult.Message.Value,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (paymentCreatedEvent == null)
        {
            _logger.LogWarning("Failed to deserialize PaymentCreatedEvent");
            return;
        }

        _logger.LogInformation(
            "🔍 Fraud Detection: Analyzing payment {PaymentId} for merchant {MerchantId}, Amount: {Amount} {Currency}",
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
        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

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
        var merchantPayments = await paymentService.GetPaymentsByMerchantIdAsync(
            paymentEvent.MerchantId,
            cancellationToken);

        var completedPaymentCount = merchantPayments.Count(p => p.Status == "Completed");

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
            var paymentService = scope.ServiceProvider.GetRequiredService<IPaymentService>();

            await paymentService.CompletePaymentAsync(paymentId, cancellationToken);

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

    private record RiskAssessment
    {
        public required bool IsApproved { get; init; }
        public required string Reason { get; init; }
    }
}
