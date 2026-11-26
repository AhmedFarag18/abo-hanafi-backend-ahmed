using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrightMinds.Infrastructure.DataAccess.Config
{
    public sealed class RawMaterialUnitConfig : IEntityTypeConfiguration<RawMaterialUnit>
    {
        public void Configure(EntityTypeBuilder<RawMaterialUnit> builder)
        {
            builder.Property(unit => unit.ConversionFactor).HasPrecision(18, 6);
            builder.Property(unit => unit.PricePerUnit).HasPrecision(18, 4);
            builder.Property(unit => unit.WeightInKg).HasPrecision(18, 6);
            builder.HasOne(unit => unit.RawMaterial)
                .WithMany(material => material.Units)
                .HasForeignKey(unit => unit.RawMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(unit => unit.Unit)
                .WithMany()
                .HasForeignKey(unit => unit.UnitId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.HasIndex(unit => new { unit.RawMaterialId, unit.UnitId }).IsUnique();
        }
    }
}
