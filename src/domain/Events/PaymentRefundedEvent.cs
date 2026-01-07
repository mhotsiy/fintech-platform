namespace FintechPlatform.Domain.Events;

/// <summary>
/// Published when a payment is refunded and the merchant's balance is updated
/// </summary>
public class PaymentRefundedEvent : IEvent
{
    public Guid EventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string EventType => "PaymentRefunded";

    public Guid PaymentId { get; private set; }
    public Guid MerchantId { get; private set; }
    public long RefundedAmountInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public long NewBalanceInMinorUnits { get; private set; }
    public Guid LedgerEntryId { get; private set; }
    public string? RefundReason { get; private set; }
    public DateTime RefundedAt { get; private set; }

    private PaymentRefundedEvent()
    {
        // For deserialization
    }

    public PaymentRefundedEvent(
        Guid paymentId,
        Guid merchantId,
        long refundedAmountInMinorUnits,
        string currency,
        long newBalanceInMinorUnits,
        Guid ledgerEntryId,
        string? refundReason = null)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        PaymentId = paymentId;
        MerchantId = merchantId;
        RefundedAmountInMinorUnits = refundedAmountInMinorUnits;
        Currency = currency;
        NewBalanceInMinorUnits = newBalanceInMinorUnits;
        LedgerEntryId = ledgerEntryId;
        RefundReason = refundReason;
        RefundedAt = DateTime.UtcNow;
    }
}
