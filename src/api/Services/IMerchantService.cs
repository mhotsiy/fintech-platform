using FintechPlatform.Api.Models.Dtos;
using FintechPlatform.Api.Models.Responses;

namespace FintechPlatform.Api.Services;

public interface IMerchantService
{
    Task<MerchantDto> CreateMerchantAsync(string name, string email, CancellationToken cancellationToken = default);
    Task<MerchantDto?> GetMerchantByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MerchantDto>> GetActiveMerchantsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<BalanceResponse>> GetMerchantBalancesAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<BalanceResponse?> GetMerchantBalanceAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default);
}
