using System.ComponentModel.DataAnnotations;

namespace FintechPlatform.Api.Models.Requests;

public class BulkCreatePaymentsRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one payment is required")]
    [MaxLength(1000, ErrorMessage = "Maximum 1000 payments per bulk request")]
    public List<CreatePaymentRequest> Payments { get; set; } = new();
}
