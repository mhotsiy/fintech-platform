using FintechPlatform.Api.Services;
using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FintechPlatform.Tests.Api.Services;

public class MerchantServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly Mock<IMerchantRepository> _merchants;
    private readonly Mock<ILogger<MerchantService>> _logger;
    private readonly MerchantService _service;

    public MerchantServiceTests()
    {
        _unitOfWork = new Mock<IUnitOfWork>();
        _merchants = new Mock<IMerchantRepository>();
        _logger = new Mock<ILogger<MerchantService>>();

        _unitOfWork.Setup(x => x.Merchants).Returns(_merchants.Object);
        _service = new MerchantService(_unitOfWork.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateMerchant_CreatesAndReturns()
    {
        var name = "PayPal Inc";
        var email = "support@paypal.com";

        _merchants
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Merchant?)null);

        _merchants
            .Setup(x => x.AddAsync(It.IsAny<Merchant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.CreateMerchantAsync(name, email);

        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Email.Should().Be(email);
        result.IsActive.Should().BeTrue();

        _merchants.Verify(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
        _merchants.Verify(x => x.AddAsync(It.Is<Merchant>(m => m.Name == name && m.Email == email), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateMerchant_DuplicateEmail_Throws()
    {
        var email = "exists@company.com";
        var existing = new Merchant("Existing Corp", email);

        _merchants
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Func<Task> act = async () => await _service.CreateMerchantAsync("New Corp", email);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{email}*already exists*");

        _merchants.Verify(x => x.AddAsync(It.IsAny<Merchant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMerchantById_Found_ReturnsMerchant()
    {
        var merchantId = Guid.NewGuid();
        var merchant = new Merchant("Square", "api@squareup.com");

        _merchants
            .Setup(x => x.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var result = await _service.GetMerchantByIdAsync(merchantId);

        result.Should().NotBeNull();
        result!.Name.Should().Be(merchant.Name);
        result.Email.Should().Be(merchant.Email);
    }

    [Fact]
    public async Task GetMerchantById_NotFound_ReturnsNull()
    {
        var merchantId = Guid.NewGuid();

        _merchants
            .Setup(x => x.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Merchant?)null);

        var result = await _service.GetMerchantByIdAsync(merchantId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveMerchants_ReturnsActiveOnly()
    {
        var activeMerchants = new List<Merchant>
        {
            new Merchant("Shopify", "dev@shopify.com"),
            new Merchant("Adyen", "tech@adyen.com")
        };

        _merchants
            .Setup(x => x.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeMerchants);

        var result = await _service.GetActiveMerchantsAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.IsActive);
    }
}
