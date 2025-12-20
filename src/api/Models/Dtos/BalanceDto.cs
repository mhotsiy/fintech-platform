namespace FintechPlatform.Api.Models.Dtos;

public class BalanceDto
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public long AvailableBalanceInMinorUnits { get; set; }
    public decimal AvailableBalanceInMajorUnits { get; set; }
    public long PendingBalanceInMinorUnits { get; set; }
    public decimal PendingBalanceInMajorUnits { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
