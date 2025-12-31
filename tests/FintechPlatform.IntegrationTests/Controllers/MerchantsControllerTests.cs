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
public class MerchantsControllerTests
{
    private HttpClient _client = null!;
    private TestWebApplicationFactory _factory = null!;

    [SetUp]
    public void Setup()
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
    public async Task CreateMerchant_ValidData_Returns201AndSavesToDb()
    {
        var request = new CreateMerchantRequest
        {
            Name = "Stripe Inc",
            Email = "payments@stripe.com"
        };

        var response = await _client.PostAsJsonAsync("/api/merchants", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var merchant = await response.Content.ReadFromJsonAsync<MerchantDto>();
        merchant.Should().NotBeNull();
        merchant!.Name.Should().Be("Stripe Inc");
        merchant.Email.Should().Be("payments@stripe.com");
        merchant.IsActive.Should().BeTrue();

        // Verify saved to database
        var db = _factory.GetDbContext();
        var dbMerchant = await db.Merchants.FindAsync(merchant.Id);
        dbMerchant.Should().NotBeNull();
        dbMerchant!.Name.Should().Be("Stripe Inc");
    }

    [Test]
    public async Task CreateMerchant_DuplicateEmail_Returns409()
    {
        var db = _factory.GetDbContext();
        var existing = new Merchant("PayPal", "support@paypal.com");
        db.Merchants.Add(existing);
        await db.SaveChangesAsync();

        var request = new CreateMerchantRequest
        {
            Name = "Another Company",
            Email = "support@paypal.com"
        };

        var response = await _client.PostAsJsonAsync("/api/merchants", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task GetMerchant_Exists_Returns200()
    {
        var db = _factory.GetDbContext();
        var merchant = new Merchant("Square", "api@squareup.com");
        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/merchants/{merchant.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MerchantDto>();
        result!.Name.Should().Be("Square");
        result.Email.Should().Be("api@squareup.com");
    }

    [Test]
    public async Task GetMerchant_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/merchants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetActiveMerchants_ReturnsOnlyActive()
    {
        var db = _factory.GetDbContext();
        var active1 = new Merchant("Adyen", "tech@adyen.com");
        var active2 = new Merchant("Klarna", "dev@klarna.com");
        var inactive = new Merchant("OldCorp", "old@corp.com");
        inactive.Deactivate();

        db.Merchants.AddRange(active1, active2, inactive);
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/merchants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var merchants = await response.Content.ReadFromJsonAsync<List<MerchantDto>>();
        merchants.Should().HaveCount(2);
        merchants.Should().OnlyContain(m => m.IsActive);
    }
}
