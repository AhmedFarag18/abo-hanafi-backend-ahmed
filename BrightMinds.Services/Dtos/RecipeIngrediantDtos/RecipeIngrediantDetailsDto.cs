using BrightMinds.Core.Models;

namespace BrightMinds.Services.Dtos.RecipeIngrediantDtos
{
    public sealed class RecipeIngrediantDetailsDto
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public int? RawMaterialId { get; set; }
        public int? SubRecipeId { get; set; }
        public string? RawMaterialName { get; set; }
        public string? SubRecipeName { get; set; }
        public decimal Quantity { get; set; }
        public BrightMinds.Core.Enums.UnitType UnitType { get; set; }
        public decimal Price { get; set; }

        public int? RawMaterialUnitId { get; set; }
        public string? RawMaterialUnitName { get; set; }
        public decimal? RawMaterialUnitWeightInKg { get; set; }
        public decimal? RawMaterialUnitPrice { get; set; }
    }
}
