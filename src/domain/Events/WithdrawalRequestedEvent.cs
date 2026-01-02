namespace FintechPlatform.Domain.Events;

/// <summary>
/// Published when a merchant requests a withdrawal.
/// This event triggers the withdrawal processing workflow.
/// </summary>
public class WithdrawalRequestedEvent : IEvent
{
    public Guid EventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string EventType => "WithdrawalRequested";

    public Guid WithdrawalId { get; private set; }
    public Guid MerchantId { get; private set; }
    public long AmountInMinorUnits { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string BankAccountNumber { get; private set; } = string.Empty;
    public string BankRoutingNumber { get; private set; } = string.Empty;

    private WithdrawalRequestedEvent()
    {
        // For deserialization
    }

    public WithdrawalRequestedEvent(
        Guid withdrawalId,
        Guid merchantId,
        long amountInMinorUnits,
        string currency,
        string bankAccountNumber,
        string bankRoutingNumber)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        WithdrawalId = withdrawalId;
        MerchantId = merchantId;
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency;
        BankAccountNumber = bankAccountNumber;
        BankRoutingNumber = bankRoutingNumber;
    }
}
