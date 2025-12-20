using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Domain.Entities;

namespace FintechPlatform.Api.Mapping;

public static class MappingExtensions
{
    public static MerchantDto ToDto(this Merchant merchant)
    {
        return new MerchantDto
        {
            Id = merchant.Id,
            Name = merchant.Name,
            Email = merchant.Email,
            CreatedAt = merchant.CreatedAt,
            IsActive = merchant.IsActive
        };
    }

    public static PaymentDto ToDto(this Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            MerchantId = payment.MerchantId,
            AmountInMinorUnits = payment.AmountInMinorUnits,
            AmountInMajorUnits = ConvertMinorToMajor(payment.AmountInMinorUnits),
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            ExternalReference = payment.ExternalReference,
            Description = payment.Description,
            CreatedAt = payment.CreatedAt,
            CompletedAt = payment.CompletedAt
        };
    }

    public static BalanceDto ToDto(this Balance balance)
    {
        return new BalanceDto
        {
            Id = balance.Id,
            MerchantId = balance.MerchantId,
            AvailableBalanceInMinorUnits = balance.AvailableBalanceInMinorUnits,
            AvailableBalanceInMajorUnits = ConvertMinorToMajor(balance.AvailableBalanceInMinorUnits),
            PendingBalanceInMinorUnits = balance.PendingBalanceInMinorUnits,
            PendingBalanceInMajorUnits = ConvertMinorToMajor(balance.PendingBalanceInMinorUnits),
            Currency = balance.Currency,
            UpdatedAt = balance.UpdatedAt
        };
    }

    private static decimal ConvertMinorToMajor(long minorUnits)
    {
        return minorUnits / 100m;
    }
}
