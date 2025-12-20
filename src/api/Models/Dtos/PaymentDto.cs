namespace FintechPlatform.Api.Models.Dtos;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public long AmountInMinorUnits { get; set; }
    public decimal AmountInMajorUnits { get; set; } // Calculated for display (e.g., 100.00)
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
