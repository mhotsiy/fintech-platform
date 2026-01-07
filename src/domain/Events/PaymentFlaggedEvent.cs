namespace FintechPlatform.Domain.Events;

/// <summary>
/// Published when a payment is flagged for manual review by fraud detection.
/// This event signals that human intervention is required before payment can be completed.
/// </summary>
public class PaymentFlaggedEvent : IEvent
{
    public Guid EventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string EventType => "PaymentFlagged";

    public Guid PaymentId { get; private set; }
    public Guid MerchantId { get; private set; }
    public long AmountInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string FlagReason { get; private set; } = string.Empty;
    public string? ExternalReference { get; private set; }
    public string? Description { get; private set; }

    private PaymentFlaggedEvent()
    {
        // For deserialization
    }

    public PaymentFlaggedEvent(
        Guid paymentId,
        Guid merchantId,
        long amountInMinorUnits,
        string currency,
        string flagReason,
        string? externalReference = null,
        string? description = null)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        PaymentId = paymentId;
        MerchantId = merchantId;
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency;
        FlagReason = flagReason;
        ExternalReference = externalReference;
        Description = description;
    }
}
