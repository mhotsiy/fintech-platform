namespace FintechPlatform.Api.Models.Responses;

public class BalanceResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public string Currency { get; set; } = string.Empty;
    public long AvailableBalanceInMinorUnits { get; set; }
    public long PendingBalanceInMinorUnits { get; set; }
    public long TotalBalanceInMinorUnits { get; set; }
    public DateTime LastUpdated { get; set; }
}
