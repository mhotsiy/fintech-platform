namespace FintechPlatform.Api.Models.Requests;

public class CreateWithdrawalRequest
{
    public long AmountInMinorUnits { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankRoutingNumber { get; set; } = string.Empty;
}
