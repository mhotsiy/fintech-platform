using FintechPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPlatform.Infrastructure.Data.Configurations;

public class BalanceConfiguration : IEntityTypeConfiguration<Balance>
{
    public void Configure(EntityTypeBuilder<Balance> builder)
    {
        builder.ToTable("balances");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");

        builder.Property(b => b.MerchantId)
            .HasColumnName("merchant_id")
            .IsRequired();

        builder.Property(b => b.AvailableBalanceInMinorUnits)
            .HasColumnName("available_balance_in_minor_units")
            .IsRequired();

        builder.Property(b => b.PendingBalanceInMinorUnits)
            .HasColumnName("pending_balance_in_minor_units")
            .IsRequired();

        builder.Property(b => b.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(b => b.Version)
            .HasColumnName("version")
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(b => new { b.MerchantId, b.Currency }).IsUnique();
    }
}
