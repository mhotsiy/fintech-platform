using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Infrastructure.Data.Repositories;

public class BalanceRepository : IBalanceRepository
{
    private readonly FintechDbContext _context;

    public BalanceRepository(FintechDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Balance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Balances
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Balance?> GetByMerchantIdAndCurrencyAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default)
    {
        return await _context.Balances
            .FirstOrDefaultAsync(b => b.MerchantId == merchantId && b.Currency == currency, cancellationToken);
    }

    public async Task<IEnumerable<Balance>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.Balances
            .Where(b => b.MerchantId == merchantId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Balance balance, CancellationToken cancellationToken = default)
    {
        await _context.Balances.AddAsync(balance, cancellationToken);
    }

    public Task UpdateAsync(Balance balance, CancellationToken cancellationToken = default)
    {
        _context.Balances.Update(balance);
        return Task.CompletedTask;
    }

    public async Task<Balance?> GetByMerchantIdAndCurrencyForUpdateAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default)
    {
        // This uses pessimistic locking to prevent race conditions
        return await _context.Balances
            .FromSqlRaw("SELECT * FROM balances WHERE merchant_id = {0} AND currency = {1} FOR UPDATE", merchantId, currency)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
