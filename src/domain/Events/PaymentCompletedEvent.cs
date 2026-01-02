namespace FintechPlatform.Domain.Events;

/// <summary>
/// Published when a payment is successfully completed and the merchant's balance has been updated.
/// This is the critical event for downstream systems (analytics, invoicing, reconciliation).
/// </summary>
public class PaymentCompletedEvent : IEvent
{
    public Guid EventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string EventType => "PaymentCompleted";

    public Guid PaymentId { get; private set; }
    public Guid MerchantId { get; private set; }
    public long AmountInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public long NewBalanceInMinorUnits { get; private set; }
    public Guid LedgerEntryId { get; private set; }
    public DateTime CompletedAt { get; private set; }

    private PaymentCompletedEvent()
    {
        // For deserialization
    }

    public PaymentCompletedEvent(
        Guid paymentId,
        Guid merchantId,
        long amountInMinorUnits,
        string currency,
        long newBalanceInMinorUnits,
        Guid ledgerEntryId)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        PaymentId = paymentId;
        MerchantId = merchantId;
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency;
        NewBalanceInMinorUnits = newBalanceInMinorUnits;
        LedgerEntryId = ledgerEntryId;
        CompletedAt = DateTime.UtcNow;
    }
}
