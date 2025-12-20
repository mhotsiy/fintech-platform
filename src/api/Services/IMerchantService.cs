using FintechPlatform.Api.Models.Dtos;

namespace FintechPlatform.Api.Services;

public interface IMerchantService
{
    Task<MerchantDto> CreateMerchantAsync(string name, string email, CancellationToken cancellationToken = default);
    Task<MerchantDto?> GetMerchantByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MerchantDto>> GetActiveMerchantsAsync(CancellationToken cancellationToken = default);
}
