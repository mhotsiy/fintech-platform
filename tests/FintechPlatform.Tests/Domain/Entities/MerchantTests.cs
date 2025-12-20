using FintechPlatform.Domain.Entities;
using FluentAssertions;

namespace FintechPlatform.Tests.Domain.Entities;

public class MerchantTests
{
    [Fact]
    public void Constructor_CreatesValidMerchant()
    {
        var merchant = new Merchant("Stripe Inc", "dev@stripe.com");

        merchant.Id.Should().NotBeEmpty();
        merchant.Name.Should().Be("Stripe Inc");
        merchant.Email.Should().Be("dev@stripe.com");
        merchant.IsActive.Should().BeTrue();
        merchant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        Action act = () => new Merchant(name!, "test@test.com");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*")
            .And.ParamName.Should().Be("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyEmail_Throws(string? email)
    {
        Action act = () => new Merchant("Company", email!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*email*")
            .And.ParamName.Should().Be("email");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user @example.com")]
    [InlineData("user..name@example.com")]
    public void Constructor_InvalidEmailFormat_Throws(string email)
    {
        Action act = () => new Merchant("Company", email);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*email format*");
    }

    [Fact]
    public void UpdateName_ChangesName()
    {
        var merchant = new Merchant("OldName", "test@test.com");
        var before = merchant.UpdatedAt;

        merchant.UpdateName("NewName");

        merchant.Name.Should().Be("NewName");
        merchant.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_EmptyName_Throws(string? name)
    {
        var merchant = new Merchant("Original", "test@test.com");

        Action act = () => merchant.UpdateName(name!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Activate_SetsIsActiveTrue()
    {
        var merchant = new Merchant("Test", "test@test.com");
        merchant.Deactivate();

        merchant.Activate();

        merchant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var merchant = new Merchant("Test", "test@test.com");

        merchant.Deactivate();

        merchant.IsActive.Should().BeFalse();
    }
}
