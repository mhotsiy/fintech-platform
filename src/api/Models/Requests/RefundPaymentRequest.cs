using System.ComponentModel.DataAnnotations;

namespace FintechPlatform.Api.Models.Requests;

public class RefundPaymentRequest
{
    /// <summary>
    /// Amount to refund in minor units (e.g., cents). If null, full amount will be refunded.
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "Refund amount must be greater than zero")]
    public long? RefundAmountInMinorUnits { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters")]
    public string? Reason { get; set; }
}
