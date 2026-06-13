using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Infrastructure.Persistence.Configurations;

public sealed class TariffModelConfiguration : IEntityTypeConfiguration<TariffModel>
{
    public void Configure(EntityTypeBuilder<TariffModel> builder)
    {
        builder.ToTable("TariffModels");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(id => id.Value, value => EntityId.From(value))
            .ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.GreenLimitKwh).HasPrecision(18, 3);
        builder.Property(t => t.BlueLimitKwh).HasPrecision(18, 3);
        builder.Property(t => t.GreenHighPriceRsd).HasPrecision(18, 4);
        builder.Property(t => t.GreenLowPriceRsd).HasPrecision(18, 4);
        builder.Property(t => t.BlueHighPriceRsd).HasPrecision(18, 4);
        builder.Property(t => t.BlueLowPriceRsd).HasPrecision(18, 4);
        builder.Property(t => t.RedHighPriceRsd).HasPrecision(18, 4);
        builder.Property(t => t.RedLowPriceRsd).HasPrecision(18, 4);
        builder.Property(t => t.PowerPriceRsdPerKw).HasPrecision(18, 4);
        builder.Property(t => t.SupplierFeeRsd).HasPrecision(18, 2);
        builder.HasIndex(t => t.IsActive);

        builder.Ignore(t => t.DomainEvents);
    }
}
