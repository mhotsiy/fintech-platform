using System.ComponentModel.DataAnnotations;

namespace FintechPlatform.Api.Models.Requests;

/// <summary>
/// Request to create a new merchant account
/// </summary>
public class CreateMerchantRequest
{
    /// <summary>
    /// Merchant business name
    /// </summary>
    /// <example>Acme Corporation</example>
    [Required(ErrorMessage = "Merchant name is required")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 255 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Merchant contact email address
    /// </summary>
    /// <example>contact@acme.com</example>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email must not exceed 255 characters")]
    public string Email { get; set; } = string.Empty;
}
