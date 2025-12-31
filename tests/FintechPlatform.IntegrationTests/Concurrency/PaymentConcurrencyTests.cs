using System.Net;
using System.Net.Http.Json;
using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Requests;
using FluentAssertions;
using NUnit.Framework;

namespace FintechPlatform.IntegrationTests.Concurrency;

[TestFixture]
public class PaymentConcurrencyTests
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
    public async Task CompletingManyPaymentsConcurrently_UpdatesBalanceCorrectly()
    {
        var merchant = await CreateMerchant("TestMerchant", "test@example.com");
        var payments = await CreateMultiplePayments(merchant.Id, 100, 1000);

        var tasks = payments.Select(p =>
            _client.PostAsync($"/api/merchants/{merchant.Id}/payments/{p.Id}/complete", null)
        );

        var responses = await Task.WhenAll(tasks);

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(100);

        var balance = await GetBalance(merchant.Id);
        balance.AvailableBalanceInMinorUnits.Should().Be(100_000);
    }

    [Test]
    public async Task CompletingSamePaymentMultipleTimes_OnlySucceedsOnce()
    {
        var merchant = await CreateMerchant("IdempotencyTest", "idempotent@example.com");
        var payment = await CreatePayment(merchant.Id, 5000);

        var tasks = Enumerable.Range(0, 10).Select(_ =>
            _client.PostAsync($"/api/merchants/{merchant.Id}/payments/{payment.Id}/complete", null)
        );

        var responses = await Task.WhenAll(tasks);

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);

        var balance = await GetBalance(merchant.Id);
        balance.AvailableBalanceInMinorUnits.Should().Be(5000);
    }

    [Test]
    public async Task DifferentAmountsConcurrently_CalculatesCorrectTotal()
    {
        var merchant = await CreateMerchant("MultiAmount", "multi@example.com");
        var amounts = new[] { 1000L, 2500L, 5000L, 10000L, 25000L };
        
        var payments = new List<PaymentDto>();
        foreach (var amount in amounts)
        {
            payments.Add(await CreatePayment(merchant.Id, amount));
        }

        var tasks = payments.Select(p =>
            _client.PostAsync($"/api/merchants/{merchant.Id}/payments/{p.Id}/complete", null)
        );

        await Task.WhenAll(tasks);

        var balance = await GetBalance(merchant.Id);
        balance.AvailableBalanceInMinorUnits.Should().Be(amounts.Sum());
    }

    private async Task<MerchantDto> CreateMerchant(string name, string email)
    {
        var request = new CreateMerchantRequest { Name = name, Email = email };
        var response = await _client.PostAsJsonAsync("/api/merchants", request);
        return (await response.Content.ReadFromJsonAsync<MerchantDto>())!;
    }

    private async Task<PaymentDto> CreatePayment(Guid merchantId, long amount)
    {
        var request = new CreatePaymentRequest
        {
            AmountInMinorUnits = amount,
            Currency = "USD",
            Description = $"Test payment"
        };

        var response = await _client.PostAsJsonAsync($"/api/merchants/{merchantId}/payments", request);
        return (await response.Content.ReadFromJsonAsync<PaymentDto>())!;
    }

    private async Task<List<PaymentDto>> CreateMultiplePayments(Guid merchantId, int count, long amount)
    {
        var payments = new List<PaymentDto>();
        for (int i = 0; i < count; i++)
        {
            payments.Add(await CreatePayment(merchantId, amount));
        }
        return payments;
    }

    private async Task<BalanceDto> GetBalance(Guid merchantId)
    {
        var response = await _client.GetAsync($"/api/merchants/{merchantId}/balance?currency=USD");
        return (await response.Content.ReadFromJsonAsync<BalanceDto>())!;
    }
}
