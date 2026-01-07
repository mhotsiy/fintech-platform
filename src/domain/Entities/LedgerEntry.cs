namespace FintechPlatform.Domain.Entities;

public enum LedgerEntryType
{
    PaymentReceived = 0,
    PaymentRefunded = 1,
    WithdrawalInitiated = 2,
    WithdrawalCompleted = 3,
    WithdrawalFailed = 4,
    WithdrawalRequested = 5,
    WithdrawalCancelled = 6,
    BalanceAdjustment = 7
}

public class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public LedgerEntryType EntryType { get; private set; }
    public long AmountInMinorUnits { get; private set; } // Positive for credit, negative for debit
    public string Currency { get; private set; } = string.Empty;
    public long BalanceAfterInMinorUnits { get; private set; }
    public Guid? RelatedPaymentId { get; private set; }
    public Guid? RelatedWithdrawalId { get; private set; }
    public string? Description { get; private set; }
    public string? Metadata { get; private set; } // JSON for additional data
    public DateTime CreatedAt { get; private set; }

    private LedgerEntry()
    {
        // EF Core constructor
    }

    public LedgerEntry(
        Guid merchantId,
        LedgerEntryType entryType,
        long amountInMinorUnits,
        string currency,
        long balanceAfterInMinorUnits,
        Guid? relatedPaymentId = null,
        Guid? relatedWithdrawalId = null,
        string? description = null,
        string? metadata = null)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("Merchant ID cannot be empty", nameof(merchantId));

        if (amountInMinorUnits == 0)
            throw new ArgumentException("Ledger entry amount cannot be zero", nameof(amountInMinorUnits));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code", nameof(currency));

        if (balanceAfterInMinorUnits < 0)
            throw new ArgumentException("Balance cannot be negative", nameof(balanceAfterInMinorUnits));

        Id = Guid.NewGuid();
        MerchantId = merchantId;
        EntryType = entryType;
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency.ToUpperInvariant();
        BalanceAfterInMinorUnits = balanceAfterInMinorUnits;
        RelatedPaymentId = relatedPaymentId;
        RelatedWithdrawalId = relatedWithdrawalId;
        Description = description;
        Metadata = metadata;
        CreatedAt = DateTime.UtcNow;
    }

    public static LedgerEntry CreateForPayment(Guid merchantId, Guid paymentId, long amountInMinorUnits, string currency, long balanceAfter, string? description = null)
    {
        return new LedgerEntry(
            merchantId,
            LedgerEntryType.PaymentReceived,
            amountInMinorUnits,
            currency,
            balanceAfter,
            relatedPaymentId: paymentId,
            description: description ?? "Payment received"
        );
    }

    public static LedgerEntry CreateForRefund(Guid merchantId, Guid paymentId, long amountInMinorUnits, string currency, long balanceAfter, string? description = null)
    {
        return new LedgerEntry(
            merchantId,
            LedgerEntryType.PaymentRefunded,
            -amountInMinorUnits, // Negative for debit
            currency,
            balanceAfter,
            relatedPaymentId: paymentId,
            description: description ?? "Payment refunded"
        );
    }

    public static LedgerEntry CreateForWithdrawal(Guid merchantId, Guid withdrawalId, long amountInMinorUnits, string currency, long balanceAfter, string? description = null)
    {
        return new LedgerEntry(
            merchantId,
            LedgerEntryType.WithdrawalInitiated,
            -amountInMinorUnits, // Negative for debit
            currency,
            balanceAfter,
            relatedWithdrawalId: withdrawalId,
            description: description ?? "Withdrawal initiated"
        );
    }
}
