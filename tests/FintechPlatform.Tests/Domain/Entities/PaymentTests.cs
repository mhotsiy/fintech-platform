using FintechPlatform.Domain.Entities;
using FluentAssertions;

namespace FintechPlatform.Tests.Domain.Entities;

public class PaymentTests
{
    private readonly Guid _merchantId = Guid.NewGuid();

    [Fact]
    public void Constructor_CreatesValidPayment()
    {
        var payment = new Payment(_merchantId, 10000, "USD", "ord-123", "iPhone purchase");

        payment.Id.Should().NotBeEmpty();
        payment.MerchantId.Should().Be(_merchantId);
        payment.AmountInMinorUnits.Should().Be(10000);
        payment.Currency.Should().Be("USD");
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.ExternalReference.Should().Be("ord-123");
        payment.Description.Should().Be("iPhone purchase");
        payment.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_EmptyMerchantId_Throws()
    {
        Action act = () => new Payment(Guid.Empty, 10000, "USD");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("merchantId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Constructor_InvalidAmount_Throws(long amount)
    {
        Action act = () => new Payment(_merchantId, amount, "USD");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("amountInMinorUnits");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyCurrency_Throws(string? currency)
    {
        Action act = () => new Payment(_merchantId, 10000, currency!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("currency");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Constructor_InvalidCurrencyLength_Throws(string currency)
    {
        Action act = () => new Payment(_merchantId, 10000, currency);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*3-letter ISO code*");
    }

    [Fact]
    public void Constructor_NormalizesCurrencyToUpperCase()
    {
        var payment = new Payment(_merchantId, 10000, "usd");

        payment.Currency.Should().Be("USD");
    }

    [Fact]
    public void Complete_SetsStatusAndTimestamp()
    {
        var payment = new Payment(_merchantId, 10000, "USD");

        payment.Complete();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.CompletedAt.Should().NotBeNull();
        payment.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    public void Complete_WhenNotPending_Throws(PaymentStatus status)
    {
        var payment = new Payment(_merchantId, 10000, "USD");
        SetPaymentStatus(payment, status);

        Action act = () => payment.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Fail_SetStatusToFailed()
    {
        var payment = new Payment(_merchantId, 10000, "USD");

        payment.Fail();

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Refund_OnCompletedPayment_Works()
    {
        var payment = new Payment(_merchantId, 10000, "USD");
        payment.Complete();

        payment.Refund();

        payment.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Failed)]
    public void Refund_OnNonCompletedPayment_Throws(PaymentStatus status)
    {
        var payment = new Payment(_merchantId, 10000, "USD");
        if (status == PaymentStatus.Failed)
            payment.Fail();

        Action act = () => payment.Refund();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed payments*");
    }

    private void SetPaymentStatus(Payment payment, PaymentStatus status)
    {
        switch (status)
        {
            case PaymentStatus.Completed:
                payment.Complete();
                break;
            case PaymentStatus.Failed:
                payment.Fail();
                break;
            case PaymentStatus.Refunded:
                payment.Complete();
                payment.Refund();
                break;
        }
    }
}
