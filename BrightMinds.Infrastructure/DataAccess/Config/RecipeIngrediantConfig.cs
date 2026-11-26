using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrightMinds.Infrastructure.DataAccess.Config
{
    public class RecipeIngrediantConfig : IEntityTypeConfiguration<RecipeIngrediant>
    {
        public void Configure(EntityTypeBuilder<RecipeIngrediant> builder)
        {
            builder.HasOne(ingredient => ingredient.RawMaterialUnit)
                .WithMany()
                .HasForeignKey(ingredient => ingredient.RawMaterialUnitId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
