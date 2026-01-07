namespace FintechPlatform.Api.Models.Responses;

public class WithdrawalResponse
{
    public Guid Id { get; set; }
    public Guid MerchantId { get; set; }
    public long AmountInMinorUnits { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankRoutingNumber { get; set; } = string.Empty;
    public string? ExternalTransactionId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
