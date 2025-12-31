using System.Net;
using System.Net.Http.Json;
using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;
using FintechPlatform.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FintechPlatform.IntegrationTests.Controllers;

[TestFixture]
public class PaymentsControllerTests
{
    private HttpClient _client = null!;
    private TestWebApplicationFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client?.Dispose();
        
        if (_factory != null)
        {
            await _factory.CleanupDatabaseAsync();
            _factory.Dispose();
        }
    }

    [Test]
    public async Task CreatePayment_ValidData_Returns201AndSavesToDb()
    {
        var merchant = await CreateMerchant("Shopify", "billing@shopify.com");

        var request = new CreatePaymentRequest
        {
            AmountInMinorUnits = 25000,
            Currency = "USD",
            ExternalReference = "order-12345",
            Description = "Product purchase"
        };

        var response = await _client.PostAsJsonAsync($"/api/merchants/{merchant.Id}/payments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>();
        payment.Should().NotBeNull();
        payment!.AmountInMinorUnits.Should().Be(25000);
        payment.Currency.Should().Be("USD");
        payment.Status.Should().Be("Pending");

        // Verify database storage
        var db = _factory.GetDbContext();
        var dbPayment = await db.Payments.FindAsync(payment.Id);
        dbPayment.Should().NotBeNull();
        dbPayment!.AmountInMinorUnits.Should().Be(25000);
    }

    [Test]
    public async Task CreatePayment_LargeAmount_StoresCorrectly()
    {
        var merchant = await CreateMerchant("BigCorp", "finance@bigcorp.com");

        var request = new CreatePaymentRequest
        {
            AmountInMinorUnits = 999999999999L, // Test INT64 storage
            Currency = "USD"
        };

        var response = await _client.PostAsJsonAsync($"/api/merchants/{merchant.Id}/payments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await response.Content.ReadFromJsonAsync<PaymentDto>();

        // Verify no data loss
        var db = _factory.GetDbContext();
        var dbPayment = await db.Payments.FindAsync(payment!.Id);
        dbPayment!.AmountInMinorUnits.Should().Be(999999999999L);
    }

    [Test]
    public async Task CreatePayment_NonExistentMerchant_Returns404()
    {
        var request = new CreatePaymentRequest
        {
            AmountInMinorUnits = 10000,
            Currency = "USD"
        };

        var response = await _client.PostAsJsonAsync($"/api/merchants/{Guid.NewGuid()}/payments", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CompletePayment_UpdatesStatusAndBalance()
    {
        var merchant = await CreateMerchant("TestMerchant", "test@merchant.com");
        var payment = await CreatePayment(merchant.Id, 50000, "USD");

        var response = await _client.PostAsync($"/api/merchants/{merchant.Id}/payments/{payment.Id}/complete", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var db = _factory.GetDbContext();
        
        // Verify payment status changed
        var dbPayment = await db.Payments.FindAsync(payment.Id);
        dbPayment!.Status.Should().Be(PaymentStatus.Completed);
        dbPayment.CompletedAt.Should().NotBeNull();

        // Verify balance updated atomically
        var balance = await db.Balances.FirstOrDefaultAsync(b => 
            b.MerchantId == merchant.Id && b.Currency == "USD");
        balance.Should().NotBeNull();
        balance!.PendingBalanceInMinorUnits.Should().Be(0);
        balance.AvailableBalanceInMinorUnits.Should().Be(50000);
    }

    [Test]
    public async Task GetPaymentsByMerchant_ReturnsCorrectPayments()
    {
        var merchant1 = await CreateMerchant("Merchant1", "m1@test.com");
        var merchant2 = await CreateMerchant("Merchant2", "m2@test.com");

        await CreatePayment(merchant1.Id, 1000, "USD");
        await CreatePayment(merchant1.Id, 2000, "USD");
        await CreatePayment(merchant2.Id, 3000, "USD");

        var response = await _client.GetAsync($"/api/merchants/{merchant1.Id}/payments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>();
        payments.Should().HaveCount(2);
        payments.Should().OnlyContain(p => p.MerchantId == merchant1.Id);
    }

    private async Task<MerchantDto> CreateMerchant(string name, string email)
    {
        var request = new CreateMerchantRequest { Name = name, Email = email };
        var response = await _client.PostAsJsonAsync("/api/merchants", request);
        return (await response.Content.ReadFromJsonAsync<MerchantDto>())!;
    }

    private async Task<PaymentDto> CreatePayment(Guid merchantId, long amount, string currency)
    {
        var request = new CreatePaymentRequest
        {
            AmountInMinorUnits = amount,
            Currency = currency
        };
        var response = await _client.PostAsJsonAsync($"/api/merchants/{merchantId}/payments", request);
        return (await response.Content.ReadFromJsonAsync<PaymentDto>())!;
    }
}
