using FintechPlatform.Domain.Repositories;
using FintechPlatform.Infrastructure.Data;
using FintechPlatform.Infrastructure.Messaging;
using FintechPlatform.Workers.Services;
using FintechPlatform.Workers.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;

var builder = Host.CreateApplicationBuilder(args);

// Configure Prometheus metrics server
builder.Services.AddMetricServer(options =>
{
    options.Port = 5002; // Expose metrics on http://localhost:5002/metrics
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddDbContext<FintechDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register repositories and unit of work
builder.Services.AddScoped<IUnitOfWork>(provider =>
{
    var context = provider.GetRequiredService<FintechDbContext>();
    return new UnitOfWork(context, connectionString);
});

// Kafka configuration
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection(KafkaSettings.SectionName));

// Register event publisher
builder.Services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

// Register Dead Letter Queue publisher
builder.Services.AddSingleton<IDeadLetterQueuePublisher, KafkaDeadLetterQueuePublisher>();

// Register background workers
builder.Services.AddHostedService<FraudDetectionWorker>();
builder.Services.AddHostedService<PaymentEventLogger>();
builder.Services.AddHostedService<WithdrawalEventLogger>();

// Register domain services that workers need
builder.Services.AddScoped<PaymentWorkflowService>();

var host = builder.Build();
host.Run();
