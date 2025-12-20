using FintechPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPlatform.Infrastructure.Data.Configurations;

public class WithdrawalConfiguration : IEntityTypeConfiguration<Withdrawal>
{
    public void Configure(EntityTypeBuilder<Withdrawal> builder)
    {
        builder.ToTable("withdrawals");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");

        builder.Property(w => w.MerchantId)
            .HasColumnName("merchant_id")
            .IsRequired();

        builder.Property(w => w.AmountInMinorUnits)
            .HasColumnName("amount_in_minor_units")
            .IsRequired();

        builder.Property(w => w.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(w => w.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(w => w.BankAccountNumber)
            .HasColumnName("bank_account_number")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.BankRoutingNumber)
            .HasColumnName("bank_routing_number")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(w => w.ExternalTransactionId)
            .HasColumnName("external_transaction_id")
            .HasMaxLength(255);

        builder.Property(w => w.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(1000);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(w => w.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(w => w.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(w => w.MerchantId);
        builder.HasIndex(w => w.Status);
    }
}
