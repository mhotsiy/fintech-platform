using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FintechPlatform.Workers.Workers;

/// <summary>
/// Worker that logs withdrawal events for monitoring/debugging
/// </summary>
public class WithdrawalEventLogger : BackgroundService
{
    private readonly ILogger<WithdrawalEventLogger> _logger;
    private readonly IConsumer<string, string> _consumer;

    public WithdrawalEventLogger(ILogger<WithdrawalEventLogger> logger, IConfiguration configuration)
    {
        _logger = logger;

        var kafkaBootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "localhost:9092";

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = "fintechplatform-withdrawal-logger",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Withdrawal event logger starting");
        _consumer.Subscribe("withdrawal-events");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    if (result?.Message != null)
                    {
                        var eventType = result.Message.Headers
                            .FirstOrDefault(h => h.Key == "event-type")?.GetValueBytes();
                        var eventTypeName = eventType != null ? System.Text.Encoding.UTF8.GetString(eventType) : "Unknown";

                        _logger.LogInformation("📋 Withdrawal Event: {EventType} - {Message}", eventTypeName, result.Message.Value);

                        _consumer.Commit(result);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming withdrawal event");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Withdrawal event logger stopping");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
        }
    }
}
