using FintechPlatform.Domain.Entities;

namespace FintechPlatform.Domain.Repositories;

public interface IMerchantRepository
{
    Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Merchant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Merchant>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
