using FintechPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPlatform.Infrastructure.Data.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");

        builder.HasKey(le => le.Id);
        builder.Property(le => le.Id).HasColumnName("id");

        builder.Property(le => le.MerchantId)
            .HasColumnName("merchant_id")
            .IsRequired();

        builder.Property(le => le.EntryType)
            .HasColumnName("entry_type")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(le => le.AmountInMinorUnits)
            .HasColumnName("amount_in_minor_units")
            .IsRequired();

        builder.Property(le => le.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(le => le.BalanceAfterInMinorUnits)
            .HasColumnName("balance_after_in_minor_units")
            .IsRequired();

        builder.Property(le => le.RelatedPaymentId)
            .HasColumnName("related_payment_id");

        builder.Property(le => le.RelatedWithdrawalId)
            .HasColumnName("related_withdrawal_id");

        builder.Property(le => le.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(le => le.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(le => le.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(le => le.MerchantId);
        builder.HasIndex(le => le.RelatedPaymentId);
        builder.HasIndex(le => le.RelatedWithdrawalId);
        builder.HasIndex(le => le.CreatedAt);
    }
}
