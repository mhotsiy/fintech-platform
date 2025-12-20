using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Infrastructure.Data.Repositories;

public class WithdrawalRepository : IWithdrawalRepository
{
    private readonly FintechDbContext _context;

    public WithdrawalRepository(FintechDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Withdrawal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Withdrawals
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Withdrawal>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.Withdrawals
            .Where(w => w.MerchantId == merchantId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Withdrawal>> GetByMerchantIdAndStatusAsync(Guid merchantId, WithdrawalStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Withdrawals
            .Where(w => w.MerchantId == merchantId && w.Status == status)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Withdrawal>> GetPendingWithdrawalsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Withdrawals
            .Where(w => w.Status == WithdrawalStatus.Pending)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Withdrawal withdrawal, CancellationToken cancellationToken = default)
    {
        await _context.Withdrawals.AddAsync(withdrawal, cancellationToken);
    }

    public Task UpdateAsync(Withdrawal withdrawal, CancellationToken cancellationToken = default)
    {
        _context.Withdrawals.Update(withdrawal);
        return Task.CompletedTask;
    }
}
