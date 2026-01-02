using System.ComponentModel.DataAnnotations;
using FintechPlatform.Domain.Enums;

namespace FintechPlatform.Api.Models.Requests;

/// <summary>
/// Request to create a new payment
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Payment amount in smallest currency unit (e.g., cents for USD, pence for GBP, yen for JPY)
    /// </summary>
    /// <example>10000</example>
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public long AmountInMinorUnits { get; set; }

    /// <summary>
    /// Currency code (ISO 4217 - 3 letter code)
    /// </summary>
    /// <example>USD</example>
    [Required(ErrorMessage = "Currency is required")]
    public Currency Currency { get; set; }

    /// <summary>
    /// Optional external reference from your system (e.g., order ID, invoice number)
    /// </summary>
    /// <example>ORDER-12345</example>
    [StringLength(255, ErrorMessage = "External reference must not exceed 255 characters")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Optional human-readable description of the payment
    /// </summary>
    /// <example>Monthly subscription payment</example>
    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
    public string? Description { get; set; }
}
