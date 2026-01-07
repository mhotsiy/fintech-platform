using FintechPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FintechPlatform.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.MerchantId)
            .HasColumnName("merchant_id")
            .IsRequired();

        builder.Property(p => p.AmountInMinorUnits)
            .HasColumnName("amount_in_minor_units")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.ExternalReference)
            .HasColumnName("external_reference")
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(p => p.CompletedBy)
            .HasColumnName("completed_by")
            .HasConversion<int?>();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(p => p.RefundedAt)
            .HasColumnName("refunded_at");

        builder.Property(p => p.RefundReason)
            .HasColumnName("refund_reason");

        builder.Property(p => p.RefundedAmountInMinorUnits)
            .HasColumnName("refunded_amount_in_minor_units");

        builder.HasIndex(p => p.MerchantId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ExternalReference);
    }
}
