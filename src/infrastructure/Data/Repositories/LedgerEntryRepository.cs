using Dapper;
using FintechPlatform.Domain.Entities;
using FintechPlatform.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FintechPlatform.Infrastructure.Data.Repositories;

public class LedgerEntryRepository : ILedgerEntryRepository
{
    private readonly FintechDbContext _context;
    private readonly string _connectionString;

    public LedgerEntryRepository(FintechDbContext context, string connectionString)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerEntries
            .FirstOrDefaultAsync(le => le.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<LedgerEntry>> GetByMerchantIdAsync(Guid merchantId, int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerEntries
            .Where(le => le.MerchantId == merchantId)
            .OrderByDescending(le => le.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LedgerEntry>> GetByMerchantIdAndDateRangeAsync(Guid merchantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerEntries
            .Where(le => le.MerchantId == merchantId && le.CreatedAt >= startDate && le.CreatedAt <= endDate)
            .OrderByDescending(le => le.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LedgerEntry>> GetByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerEntries
            .Where(le => le.RelatedPaymentId == paymentId)
            .OrderByDescending(le => le.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LedgerEntry>> GetByWithdrawalIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerEntries
            .Where(le => le.RelatedWithdrawalId == withdrawalId)
            .OrderByDescending(le => le.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken = default)
    {
        await _context.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);
    }

    public async Task<long> GetBalanceFromLedgerAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default)
    {
        // Use Dapper for raw SQL query for better performance on aggregations
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT COALESCE(SUM(amount_in_minor_units), 0) as Balance
            FROM ledger_entries
            WHERE merchant_id = @MerchantId AND currency = @Currency";

        var balance = await connection.QuerySingleAsync<long>(sql, new { MerchantId = merchantId, Currency = currency });
        return balance;
    }
}
