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

        [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
        public decimal Quantity { get; set; }

        [Required]
        public UnitType UnitType { get; set; }

        public int? RawMaterialUnitId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IngredientType == IngredientType.RawMaterial)
            {
                if (!RawMaterialId.HasValue)
                    yield return new ValidationResult("RawMaterialId is required.", new[] { nameof(RawMaterialId) });
                if (RawMaterialUnitId.HasValue && RawMaterialUnitId.Value <= 0)
                    yield return new ValidationResult("RawMaterialUnitId must be greater than zero.", new[] { nameof(RawMaterialUnitId) });
            }
            else if (IngredientType == IngredientType.SubRecipe)
            {
                if (!SubRecipeId.HasValue)
                    yield return new ValidationResult("SubRecipeId is required.", new[] { nameof(SubRecipeId) });
                if (RawMaterialUnitId.HasValue)
                    yield return new ValidationResult("RawMaterialUnitId is invalid for sub-recipe ingredients.", new[] { nameof(RawMaterialUnitId) });
            }
        }
    }
}
