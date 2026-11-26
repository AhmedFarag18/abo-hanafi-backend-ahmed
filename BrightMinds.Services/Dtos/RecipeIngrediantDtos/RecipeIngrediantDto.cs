using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.RecipeIngrediantDtos
{
    public class RecipeIngrediantDto : IValidatableObject
    {
        public int? RawMaterialId { get; set; }
        public int? SubRecipeId { get; set; }
        [Required]
        public IngredientType IngredientType { get; set; }
        [Required]
        [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
        public decimal Quantity { get; set; }
        [Required]
        public UnitType UnitType { get; set; }

        // Optional for compatibility with existing requests; required to use a material-specific unit.
        public int? RawMaterialUnitId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IngredientType == IngredientType.SubRecipe && SubRecipeId == null)
                yield return new ValidationResult("SubRecipe ID is required when ingredient type is sub recipe!", new[] { nameof(SubRecipeId) });
            else if (IngredientType == IngredientType.RawMaterial && RawMaterialId == null)
                yield return new ValidationResult("Raw Material ID is required when ingredient type is Raw Material!", new[] { nameof(RawMaterialId) });
            else if ((RawMaterialId == null && SubRecipeId == null) || (RawMaterialId != null && SubRecipeId != null))
                yield return new ValidationResult("The recipe ingredient must be either a raw material or a sub recipe.", new[] { nameof(RawMaterialId), nameof(SubRecipeId) });
            if (RawMaterialUnitId.HasValue && (IngredientType != IngredientType.RawMaterial || !RawMaterialId.HasValue))
                yield return new ValidationResult("A raw-material unit can only be selected for a raw-material ingredient.", new[] { nameof(RawMaterialUnitId) });
        }
    }
}
