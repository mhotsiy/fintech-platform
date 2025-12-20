using FintechPlatform.Domain.Entities;

namespace FintechPlatform.Domain.Repositories;

public interface IWithdrawalRepository
{
    Task<Withdrawal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Withdrawal>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Withdrawal>> GetByMerchantIdAndStatusAsync(Guid merchantId, WithdrawalStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Withdrawal>> GetPendingWithdrawalsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Withdrawal withdrawal, CancellationToken cancellationToken = default);
    Task UpdateAsync(Withdrawal withdrawal, CancellationToken cancellationToken = default);
}
