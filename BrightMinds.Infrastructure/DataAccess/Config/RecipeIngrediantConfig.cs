using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrightMinds.Infrastructure.DataAccess.Config
{
    public class RecipeIngrediantConfig : IEntityTypeConfiguration<RecipeIngrediant>
    {
        public void Configure(EntityTypeBuilder<RecipeIngrediant> builder)
        {
            builder.HasOne(x => x.RawMaterialUnit)
                .WithMany()
                .HasForeignKey(x => x.RawMaterialUnitId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
