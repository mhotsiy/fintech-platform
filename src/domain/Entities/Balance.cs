namespace FintechPlatform.Domain.Entities;

public class Balance
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public long AvailableBalanceInMinorUnits { get; private set; }
    public long PendingBalanceInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int Version { get; private set; } // Optimistic concurrency control

    private Balance()
    {
        // EF Core constructor
    }

    public Balance(Guid merchantId, string currency)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("Merchant ID cannot be empty", nameof(merchantId));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code", nameof(currency));

        Id = Guid.NewGuid();
        MerchantId = merchantId;
        AvailableBalanceInMinorUnits = 0;
        PendingBalanceInMinorUnits = 0;
        Currency = currency.ToUpperInvariant();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        Version = 0;
    }

    public void AddToAvailableBalance(long amountInMinorUnits)
    {
        if (amountInMinorUnits <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amountInMinorUnits));

        AvailableBalanceInMinorUnits += amountInMinorUnits;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void AddToPendingBalance(long amountInMinorUnits)
    {
        if (amountInMinorUnits <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amountInMinorUnits));

        PendingBalanceInMinorUnits += amountInMinorUnits;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void MovePendingToAvailable(long amountInMinorUnits)
    {
        if (amountInMinorUnits <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amountInMinorUnits));

        if (PendingBalanceInMinorUnits < amountInMinorUnits)
            throw new InvalidOperationException("Insufficient pending balance");

        PendingBalanceInMinorUnits -= amountInMinorUnits;
        AvailableBalanceInMinorUnits += amountInMinorUnits;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void DeductFromAvailableBalance(long amountInMinorUnits)
    {
        if (amountInMinorUnits <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amountInMinorUnits));

        if (AvailableBalanceInMinorUnits < amountInMinorUnits)
            throw new InvalidOperationException("Insufficient available balance");

        AvailableBalanceInMinorUnits -= amountInMinorUnits;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void DeductFromPendingBalance(long amountInMinorUnits)
    {
        if (amountInMinorUnits <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amountInMinorUnits));

        if (PendingBalanceInMinorUnits < amountInMinorUnits)
            throw new InvalidOperationException("Insufficient pending balance");

        PendingBalanceInMinorUnits -= amountInMinorUnits;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }
}
