namespace FintechPlatform.Domain.Events;

/// <summary>
/// Published when a new payment is created (but not yet completed).
/// This event signals that a payment intent exists in the system.
/// </summary>
public class PaymentCreatedEvent : IEvent
{
    public Guid EventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string EventType => "PaymentCreated";

    public Guid PaymentId { get; private set; }
    public Guid MerchantId { get; private set; }
    public long AmountInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? ExternalReference { get; private set; }
    public string? Description { get; private set; }

    private PaymentCreatedEvent()
    {
        // For deserialization
    }

    public PaymentCreatedEvent(
        Guid paymentId,
        Guid merchantId,
        long amountInMinorUnits,
        string currency,
        string? externalReference,
        string? description)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        PaymentId = paymentId;
        MerchantId = merchantId;
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency;
        ExternalReference = externalReference;
        Description = description;
    }
}
