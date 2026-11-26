using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrightMinds.Infrastructure.DataAccess.Config
{
    public class RawMaterialUnitConfig : IEntityTypeConfiguration<RawMaterialUnit>
    {
        public void Configure(EntityTypeBuilder<RawMaterialUnit> builder)
        {
            builder.Property(x => x.ConversionFactor).HasPrecision(18, 6);
            builder.Property(x => x.PricePerUnit).HasPrecision(18, 4);
            builder.Property(x => x.WeightInKg).HasPrecision(18, 6);
            builder.HasOne(x => x.RawMaterial)
                .WithMany(x => x.Units)
                .HasForeignKey(x => x.RawMaterialId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(x => x.Unit)
                .WithMany()
                .HasForeignKey(x => x.UnitId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.RawMaterialId, x.UnitId }).IsUnique();
        }
    }
}
