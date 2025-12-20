namespace FintechPlatform.Domain.Entities;

public enum WithdrawalStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

public class Withdrawal
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public long AmountInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public WithdrawalStatus Status { get; private set; }
    public string BankAccountNumber { get; private set; } = string.Empty;
    public string BankRoutingNumber { get; private set; } = string.Empty;
    public string? ExternalTransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Withdrawal()
    {
        // EF Core constructor
    }

    public Withdrawal(Guid merchantId, long amountInMinorUnits, string currency, string bankAccountNumber, string bankRoutingNumber)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("Merchant ID cannot be empty", nameof(merchantId));

        if (amountInMinorUnits <= 0)
            throw new ArgumentException("Withdrawal amount must be greater than zero", nameof(amountInMinorUnits));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code", nameof(currency));

        if (string.IsNullOrWhiteSpace(bankAccountNumber))
            throw new ArgumentException("Bank account number cannot be empty", nameof(bankAccountNumber));

        if (string.IsNullOrWhiteSpace(bankRoutingNumber))
            throw new ArgumentException("Bank routing number cannot be empty", nameof(bankRoutingNumber));

        Id = Guid.NewGuid();
        MerchantId = merchantId;
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency.ToUpperInvariant();
        Status = WithdrawalStatus.Pending;
        BankAccountNumber = bankAccountNumber;
        BankRoutingNumber = bankRoutingNumber;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessing(string externalTransactionId)
    {
        if (Status != WithdrawalStatus.Pending)
            throw new InvalidOperationException($"Cannot process withdrawal in {Status} status");

        if (string.IsNullOrWhiteSpace(externalTransactionId))
            throw new ArgumentException("External transaction ID cannot be empty", nameof(externalTransactionId));

        Status = WithdrawalStatus.Processing;
        ExternalTransactionId = externalTransactionId;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != WithdrawalStatus.Processing)
            throw new InvalidOperationException($"Cannot complete withdrawal in {Status} status");

        Status = WithdrawalStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        if (Status == WithdrawalStatus.Completed || Status == WithdrawalStatus.Cancelled)
            throw new InvalidOperationException($"Cannot fail withdrawal in {Status} status");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason cannot be empty", nameof(reason));

        Status = WithdrawalStatus.Failed;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != WithdrawalStatus.Pending)
            throw new InvalidOperationException($"Can only cancel pending withdrawals. Current status: {Status}");

        Status = WithdrawalStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
