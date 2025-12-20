using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Infrastructure.Data.Repositories;

public class MerchantRepository : IMerchantRepository
{
    private readonly FintechDbContext _context;

    public MerchantRepository(FintechDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Merchant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Merchants
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Merchant?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Merchants
            .FirstOrDefaultAsync(m => m.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Merchant>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Merchants
            .Where(m => m.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        await _context.Merchants.AddAsync(merchant, cancellationToken);
    }

    public Task UpdateAsync(Merchant merchant, CancellationToken cancellationToken = default)
    {
        _context.Merchants.Update(merchant);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Merchants.AnyAsync(m => m.Id == id, cancellationToken);
    }
}
