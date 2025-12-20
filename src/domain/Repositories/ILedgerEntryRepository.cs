using FintechPlatform.Domain.Entities;

namespace FintechPlatform.Domain.Repositories;

public interface ILedgerEntryRepository
{
    Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<LedgerEntry>> GetByMerchantIdAsync(Guid merchantId, int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
    Task<IEnumerable<LedgerEntry>> GetByMerchantIdAndDateRangeAsync(Guid merchantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<LedgerEntry>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LedgerEntry>> GetByWithdrawalIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default);
    Task AddAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken = default);
    Task<long> GetBalanceFromLedgerAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default);
}
