using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Properties;
using SmartMetering.Domain.Users;

namespace SmartMetering.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .ValueGeneratedNever();

        builder.Property(i => i.ConsumerId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();

        builder.Property(i => i.PropertyId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();

        builder.Property(i => i.MeterId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();

        builder.Property(i => i.TariffModelId)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .IsRequired();

        builder.Property(i => i.SerialNumber).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.TextBlobName).HasMaxLength(300).IsRequired();
        builder.Property(i => i.PdfBlobName).HasMaxLength(300).IsRequired();

        builder.Property(i => i.HighTariffKwh).HasPrecision(18, 3);
        builder.Property(i => i.LowTariffKwh).HasPrecision(18, 3);
        builder.Property(i => i.GreenHighKwh).HasPrecision(18, 3);
        builder.Property(i => i.GreenLowKwh).HasPrecision(18, 3);
        builder.Property(i => i.BlueHighKwh).HasPrecision(18, 3);
        builder.Property(i => i.BlueLowKwh).HasPrecision(18, 3);
        builder.Property(i => i.RedHighKwh).HasPrecision(18, 3);
        builder.Property(i => i.RedLowKwh).HasPrecision(18, 3);
        builder.Property(i => i.GreenAmountRsd).HasPrecision(18, 2);
        builder.Property(i => i.BlueAmountRsd).HasPrecision(18, 2);
        builder.Property(i => i.RedAmountRsd).HasPrecision(18, 2);
        builder.Property(i => i.FixedAmountRsd).HasPrecision(18, 2);
        builder.Property(i => i.TotalAmountRsd).HasPrecision(18, 2);

        builder.HasIndex(i => new { i.MeterId, i.Year, i.Month }).IsUnique();
        builder.HasIndex(i => new { i.ConsumerId, i.PropertyId, i.IssuedAtUtc });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.ConsumerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SmartMeter>()
            .WithMany()
            .HasForeignKey(i => i.MeterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TariffModel>()
            .WithMany()
            .HasForeignKey(i => i.TariffModelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(i => i.TotalKwh);
        builder.Ignore(i => i.GreenKwh);
        builder.Ignore(i => i.BlueKwh);
        builder.Ignore(i => i.RedKwh);
        builder.Ignore(i => i.DomainEvents);
    }
}
