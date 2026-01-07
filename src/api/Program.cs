using FintechPlatform.Api.BackgroundServices;
using FintechPlatform.Api.Hubs;
using FintechPlatform.Api.Middleware;
using FintechPlatform.Api.Services;
using FintechPlatform.Domain.Repositories;
using FintechPlatform.Infrastructure.Data;
using FintechPlatform.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Memory cache for idempotency
builder.Services.AddMemoryCache();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Allow common localhost origins for development
        policy.SetIsOriginAllowed(origin => 
            {
                // In development, allow localhost and 127.0.0.1 on any port
                if (builder.Environment.IsDevelopment())
                {
                    var uri = new Uri(origin);
                    return uri.Host == "localhost" || uri.Host == "127.0.0.1";
                }
                
                // In production, check configured origins
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? Array.Empty<string>();
                return allowedOrigins.Contains(origin);
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("*");
    });
});

// SignalR for real-time notifications
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Enable for debugging
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

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

// Register idempotency store
builder.Services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();

// Register services
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWithdrawalService, WithdrawalService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Background services
// NotificationConsumer runs in API to broadcast real-time notifications via SignalR
builder.Services.AddHostedService<NotificationConsumer>();

// Note: Other workers (FraudDetection, PaymentWorkflow, etc.) run in separate Workers project

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Global exception handler should be first
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must be before other middleware
app.UseCors("AllowAll");

// Add idempotency middleware
app.UseMiddleware<IdempotencyMiddleware>();

// Don't redirect to HTTPS in development (we're using HTTP on port 5153)
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
