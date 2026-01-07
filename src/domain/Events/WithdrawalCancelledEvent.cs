namespace FintechPlatform.Domain.Events;

/// <summary>
/// Published when a withdrawal is cancelled by the merchant.
/// </summary>
public class WithdrawalCancelledEvent : IEvent
{
    public Guid EventId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType => "WithdrawalCancelled";

    public Guid WithdrawalId { get; set; }
    public Guid MerchantId { get; set; }
    public long AmountInMinorUnits { get; set; }
    public string Currency { get; set; } = string.Empty;
}
