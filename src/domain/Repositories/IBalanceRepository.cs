using FintechPlatform.Domain.Entities;

namespace FintechPlatform.Domain.Repositories;

public interface IBalanceRepository
{
    Task<Balance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Balance?> GetByMerchantIdAndCurrencyAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default);
    Task<IEnumerable<Balance>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task AddAsync(Balance balance, CancellationToken cancellationToken = default);
    Task UpdateAsync(Balance balance, CancellationToken cancellationToken = default);
    Task<Balance?> GetByMerchantIdAndCurrencyForUpdateAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default);
}
