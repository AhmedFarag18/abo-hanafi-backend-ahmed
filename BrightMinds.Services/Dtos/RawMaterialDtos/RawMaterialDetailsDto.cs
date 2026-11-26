using BrightMinds.Core.Enums;

namespace BrightMinds.Services.Dtos.RawMaterialDtos
{
    public class RawMaterialDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public HandlingType HandlingType { get; set; }
        public string? ContainerUnitName { get; set; }
        public decimal ContainerSize { get; set; }
        public UnitType UnitType { get; set; }
        public string? SubUnitName { get; set; }
        public decimal? UnitWeightInKg { get; set; }
        public decimal TotalContainerPrice { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal? TotalWeightPerContainer { get; set; }
        public bool IsWeightBased { get; set; }
        public decimal NormalStockLevel { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public bool IsRestaurantSupply { get; set; }
        public bool IsMeat { get; set; }
        public decimal? MeatFactor { get; set; }
        public bool IsMultiplierLocked { get; set; }
        public decimal? PricePerKg { get; set; }
        public string? PriceDisplayUnit { get; set; }
        public decimal? PricePerPiece { get; set; }

        // Legacy fields are maintained in responses for older frontend builds.
        public string? InnerUnitName { get; set; }
        public decimal InnerUnitsPerSubUnit { get; set; }
        public decimal? InnerUnitWeightInKg { get; set; }
        public decimal? PricePerInnerUnit { get; set; }
        public decimal? PricePerSubUnit { get; set; }

        public string? PurchaseUnitName { get; set; }
        public string? MiddleUnitName { get; set; }
        public decimal? MiddleUnitsPerPurchaseUnit { get; set; }
        public string? SmallestUnitName { get; set; }
        public decimal? SmallestUnitsPerMiddleUnit { get; set; }
        public decimal? SmallestUnitWeightInKg { get; set; }
        public decimal? PricePerPurchaseUnit { get; set; }
        public decimal? PricePerMiddleUnit { get; set; }
        public decimal? PricePerSmallestUnit { get; set; }

        // Preview values only; never written back to the catalog prices.
        public decimal? CalculatedPricePerMiddleUnit =>
            PricePerPurchaseUnit.HasValue && MiddleUnitsPerPurchaseUnit > 0
                ? PricePerPurchaseUnit.Value / MiddleUnitsPerPurchaseUnit.Value
                : null;

        public decimal? CalculatedPricePerSmallestUnit =>
            PricePerMiddleUnit.HasValue && SmallestUnitsPerMiddleUnit > 0
                ? PricePerMiddleUnit.Value / SmallestUnitsPerMiddleUnit.Value
                : null;
    }
}
