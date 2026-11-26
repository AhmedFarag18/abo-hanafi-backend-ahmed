using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrightMinds.Infrastructure.DataAccess.Config
{
    public class SupplyItemConfig : IEntityTypeConfiguration<SupplyItem>
    {
        public void Configure(EntityTypeBuilder<SupplyItem> builder)
        {
            builder.Property(item => item.UnitPrice).HasPrecision(18, 4);
            builder.Property(item => item.ValuationPrice).HasPrecision(18, 4);
            builder.HasOne(item => item.RawMaterialUnit)
                .WithMany()
                .HasForeignKey(item => item.RawMaterialUnitId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
