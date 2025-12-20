using System.ComponentModel.DataAnnotations;

namespace FintechPlatform.Api.Models.Requests;

public class CreatePaymentRequest
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public long AmountInMinorUnits { get; set; }

    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter ISO code")]
    [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be 3 uppercase letters")]
    public string Currency { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "External reference must not exceed 255 characters")]
    public string? ExternalReference { get; set; }

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }
}
