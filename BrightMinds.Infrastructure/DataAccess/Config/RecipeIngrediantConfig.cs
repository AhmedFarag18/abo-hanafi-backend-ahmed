using BrightMinds.Core.Models;

namespace BrightMinds.Infrastructure.DataAccess.Config
{
    public sealed class RecipeIngrediantConfig : Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<RecipeIngrediant>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<RecipeIngrediant> builder)
        {
            builder.HasOne(ingredient => ingredient.RawMaterialUnit)
                .WithMany()
                .HasForeignKey(ingredient => ingredient.RawMaterialUnitId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.NoAction);
        }
    }
}
