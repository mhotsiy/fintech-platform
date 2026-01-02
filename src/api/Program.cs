using FintechPlatform.Api.BackgroundServices;
using FintechPlatform.Api.Services;
using FintechPlatform.Domain.Repositories;
using FintechPlatform.Infrastructure.Data;
using FintechPlatform.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings instead of integers
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fintech Platform API",
        Version = "v1",
        Description = "A fintech payment platform with ACID transactions, event-driven architecture using Kafka, and ledger-based balance tracking",
        Contact = new OpenApiContact
        {
            Name = "Fintech Platform Team"
        }
    });

    // Enable XML comments for better documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

// Register background services (event consumers)
builder.Services.AddHostedService<PaymentEventConsumer>();
builder.Services.AddHostedService<WithdrawalEventConsumer>();

// Register services
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowAll");
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
