namespace FintechPlatform.Domain.Entities;

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

public enum CompletionSource
{
    Manual = 0,
    FraudDetection = 1
}

public class Payment
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public long AmountInMinorUnits { get; private set; } // Store in cents
    public string Currency { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public CompletionSource? CompletedBy { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public string? RefundReason { get; private set; }
    public long? RefundedAmountInMinorUnits { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Payment()
    {
        // EF Core constructor
    }

    public Payment(Guid merchantId, long amountInMinorUnits, string currency, string? externalReference = null, string? description = null)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("Merchant ID cannot be empty", nameof(merchantId));

        if (amountInMinorUnits <= 0)
            throw new ArgumentException("Payment amount must be greater than zero", nameof(amountInMinorUnits));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code", nameof(currency));

        Id = Guid.NewGuid();
        MerchantId = merchantId;
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency.ToUpperInvariant();
        Status = PaymentStatus.Pending;
        ExternalReference = externalReference;
        Description = description;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(CompletionSource completionSource = CompletionSource.Manual)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot complete payment in {Status} status");

        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        CompletedBy = completionSource;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot fail payment in {Status} status");

        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Refund(long refundAmountInMinorUnits, string? reason = null)
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Only completed payments can be refunded");

        if (refundAmountInMinorUnits <= 0)
            throw new ArgumentException("Refund amount must be greater than zero", nameof(refundAmountInMinorUnits));

        if (refundAmountInMinorUnits > AmountInMinorUnits)
            throw new ArgumentException("Refund amount cannot exceed payment amount", nameof(refundAmountInMinorUnits));

        Status = PaymentStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        RefundReason = reason;
        RefundedAmountInMinorUnits = refundAmountInMinorUnits;
        UpdatedAt = DateTime.UtcNow;
    }
}
