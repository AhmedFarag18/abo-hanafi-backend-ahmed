using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrightMinds.Core.Models
{
    public class RecipeIngrediant : BaseEntity
    {
        public int RecipeId { get; set; }
        public virtual Recipe Recipe { get; set; } = null!;
        public int? RawMaterialId { get; set; }
        public virtual RawMaterial RawMaterial { get; set; } = null!;
        public int? SubRecipeId { get; set; }
        public Recipe SubRecipe { get; set; } = null!;
        public IngredientType IngredientType { get; set; }
        public decimal Quantity { get; set; }
        public UnitType UnitType { get; set; }

        // Optional additive unit reference; null preserves existing recipe rows/contracts.
        public int? RawMaterialUnitId { get; set; }
        public virtual RawMaterialUnit? RawMaterialUnit { get; set; }
    }

    public static class RecipeIngrediantExtensions
    {
        public static bool IsValid(this RecipeIngrediant recipeIngredient, out string error)
        {
            if (recipeIngredient.IngredientType == IngredientType.SubRecipe && recipeIngredient.SubRecipeId == null)
            {
                error = "SubRecipe ID is required when ingredient type is sub recipe!";
                return false;
            }
            if (recipeIngredient.IngredientType == IngredientType.RawMaterial && recipeIngredient.RawMaterialId == null)
            {
                error = "Raw Material ID is required when ingredient type is Raw Material!";
                return false;
            }
            if ((recipeIngredient.RawMaterialId == null && recipeIngredient.SubRecipeId == null) ||
                (recipeIngredient.RawMaterialId != null && recipeIngredient.SubRecipeId != null))
            {
                error = "The recipie ingredient must me either Raw Material Or Sub Recipe!";
                return false;
            }
            if (recipeIngredient.RawMaterialUnitId.HasValue && recipeIngredient.RawMaterialId == null)
            {
                error = "A raw-material unit can only be set for a raw-material ingredient.";
                return false;
            }

            error = null!;
            return true;
        }
    }
}
