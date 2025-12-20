using FintechPlatform.Domain.Entities;
using FluentAssertions;

namespace FintechPlatform.Tests.Domain.Entities;

public class BalanceTests
{
    private readonly Guid _merchantId = Guid.NewGuid();

    [Fact]
    public void Constructor_StartsWithZeroBalance()
    {
        var balance = new Balance(_merchantId, "USD");

        balance.Id.Should().NotBeEmpty();
        balance.MerchantId.Should().Be(_merchantId);
        balance.Currency.Should().Be("USD");
        balance.AvailableBalanceInMinorUnits.Should().Be(0);
        balance.PendingBalanceInMinorUnits.Should().Be(0);
        balance.Version.Should().Be(0);
    }

    [Fact]
    public void Constructor_EmptyMerchantId_Throws()
    {
        Action act = () => new Balance(Guid.Empty, "USD");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Merchant ID*");
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("eur")]
    [InlineData("gbp")]
    public void Constructor_NormalizesCurrency(string currency)
    {
        var balance = new Balance(_merchantId, currency);

        balance.Currency.Should().Be(currency.ToUpperInvariant());
    }

    [Fact]
    public void AddToAvailableBalance_IncreasesBalance()
    {
        var balance = new Balance(_merchantId, "USD");

        balance.AddToAvailableBalance(10000);

        balance.AvailableBalanceInMinorUnits.Should().Be(10000);
        balance.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void AddToAvailableBalance_InvalidAmount_Throws(long amount)
    {
        var balance = new Balance(_merchantId, "USD");

        Action act = () => balance.AddToAvailableBalance(amount);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void AddToPendingBalance_IncreasesPending()
    {
        var balance = new Balance(_merchantId, "USD");

        balance.AddToPendingBalance(5000);

        balance.PendingBalanceInMinorUnits.Should().Be(5000);
        balance.AvailableBalanceInMinorUnits.Should().Be(0);
        balance.Version.Should().Be(1);
    }

    [Fact]
    public void DeductFromAvailableBalance_DecreasesBalance()
    {
        var balance = new Balance(_merchantId, "USD");
        balance.AddToAvailableBalance(10000);
        var versionAfterAdd = balance.Version;

        balance.DeductFromAvailableBalance(3000);

        balance.AvailableBalanceInMinorUnits.Should().Be(7000);
        balance.Version.Should().Be(versionAfterAdd + 1);
    }

    [Fact]
    public void DeductFromAvailableBalance_InsufficientFunds_Throws()
    {
        var balance = new Balance(_merchantId, "USD");
        balance.AddToAvailableBalance(5000);

        Action act = () => balance.DeductFromAvailableBalance(10000);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient available balance*");
    }

    [Fact]
    public void MovePendingToAvailable_TransfersAmount()
    {
        var balance = new Balance(_merchantId, "USD");
        balance.AddToPendingBalance(10000);
        balance.AddToAvailableBalance(5000);

        balance.MovePendingToAvailable(7000);

        balance.PendingBalanceInMinorUnits.Should().Be(3000);
        balance.AvailableBalanceInMinorUnits.Should().Be(12000);
    }

    [Fact]
    public void MovePendingToAvailable_InsufficientPending_Throws()
    {
        var balance = new Balance(_merchantId, "USD");
        balance.AddToPendingBalance(5000);

        Action act = () => balance.MovePendingToAvailable(10000);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient pending balance*");
    }

    [Fact]
    public void VersionIncrementsOnEachOperation()
    {
        var balance = new Balance(_merchantId, "USD");
        balance.Version.Should().Be(0);

        balance.AddToAvailableBalance(10000);
        balance.Version.Should().Be(1);

        balance.AddToPendingBalance(5000);
        balance.Version.Should().Be(2);

        balance.DeductFromAvailableBalance(2000);
        balance.Version.Should().Be(3);

        balance.MovePendingToAvailable(3000);
        balance.Version.Should().Be(4);
    }
}
