namespace FintechPlatform.Domain.Repositories;

public interface IUnitOfWork : IDisposable
{
    IMerchantRepository Merchants { get; }
    IPaymentRepository Payments { get; }
    IBalanceRepository Balances { get; }
    IWithdrawalRepository Withdrawals { get; }
    ILedgerEntryRepository LedgerEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
