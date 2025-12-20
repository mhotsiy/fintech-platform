using FintechPlatform.Domain.Repositories;
using FintechPlatform.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace FintechPlatform.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly FintechDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(FintechDbContext context, string connectionString)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        Merchants = new MerchantRepository(_context);
        Payments = new PaymentRepository(_context);
        Balances = new BalanceRepository(_context);
        Withdrawals = new WithdrawalRepository(_context);
        LedgerEntries = new LedgerEntryRepository(_context, connectionString);
    }

    public IMerchantRepository Merchants { get; }
    public IPaymentRepository Payments { get; }
    public IBalanceRepository Balances { get; }
    public IWithdrawalRepository Withdrawals { get; }
    public ILedgerEntryRepository LedgerEntries { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction started");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction started");
        }

        await _transaction.RollbackAsync(cancellationToken);
        _transaction.Dispose();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
