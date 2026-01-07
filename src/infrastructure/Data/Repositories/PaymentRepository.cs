using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Infrastructure.Data.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly FintechDbContext _context;

    public PaymentRepository(FintechDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByMerchantIdAndStatusAsync(Guid merchantId, PaymentStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.MerchantId == merchantId && p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetByMerchantIdWithFilterAsync(Guid merchantId, PaymentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.Where(p => p.MerchantId == merchantId);

        // Date range filter
        if (filter.DateFrom.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            // Include full day
            var dateTo = filter.DateTo.Value.Date.AddDays(1);
            query = query.Where(p => p.CreatedAt < dateTo);
        }

        // Amount range filter
        if (filter.MinAmount.HasValue)
        {
            query = query.Where(p => p.AmountInMinorUnits >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount.HasValue)
        {
            query = query.Where(p => p.AmountInMinorUnits <= filter.MaxAmount.Value);
        }

        // Status filter
        if (filter.Statuses != null && filter.Statuses.Any())
        {
            query = query.Where(p => filter.Statuses.Contains(p.Status));
        }

        // Text search (description or external reference)
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchTerm = filter.Search.ToLower();
            query = query.Where(p => 
                (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                (p.ExternalReference != null && p.ExternalReference.ToLower().Contains(searchTerm)));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Payments.Update(payment);
        return Task.CompletedTask;
    }
}
