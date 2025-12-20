using FintechPlatform.Domain.Entities;
using FintechPlatform.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FintechPlatform.Infrastructure.Data;

public class FintechDbContext : DbContext
{
    public FintechDbContext(DbContextOptions<FintechDbContext> options) : base(options)
    {
    }

    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Balance> Balances => Set<Balance>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MerchantConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new BalanceConfiguration());
        modelBuilder.ApplyConfiguration(new WithdrawalConfiguration());
        modelBuilder.ApplyConfiguration(new LedgerEntryConfiguration());
    }
}
