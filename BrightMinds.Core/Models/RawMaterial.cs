using BrightMinds.Core.Enums;

namespace BrightMinds.Core.Models
{
    public class RawMaterial : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public HandlingType HandlingType { get; set; }

        // Legacy contract retained for existing inventory, supply and frontend integrations.
        public string? ContainerUnitName { get; set; }
        public decimal ContainerSize { get; set; }
        public UnitType UnitType { get; set; }
        public string? SubUnitName { get; set; }
        public decimal? UnitWeightInKg { get; set; }
        public decimal TotalContainerPrice { get; set; }
        public decimal NormalStockLevel { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; } = null!;

        public decimal PricePerUnit => ContainerSize > 0 ? TotalContainerPrice / ContainerSize : 0;
        public decimal? TotalWeightPerContainer => UnitWeightInKg.HasValue ? ContainerSize * UnitWeightInKg.Value : null;
        public bool IsWeightBased => UnitType == UnitType.kg || UnitType == UnitType.g;
        public bool IsCountBased => !IsWeightBased;
        public bool HasWeight => UnitWeightInKg.HasValue && UnitWeightInKg.Value > 0;
        public bool IsRestaurantSupply { get; set; }
        public bool IsMeat { get; set; }
        public decimal? MeatFactor { get; set; }
        public bool IsMultiplierLocked { get; set; }
        public decimal? PricePerKg { get; set; }
        public decimal? PricePerPiece { get; set; }
        public virtual ICollection<RawMaterialUnit> Units { get; set; } = new List<RawMaterialUnit>();

        // Legacy smallest-unit fields.
        public string? InnerUnitName { get; set; }
        public decimal InnerUnitsPerSubUnit { get; set; } = 1;
        public decimal? InnerUnitWeightInKg { get; set; }
        public decimal? PricePerInnerUnit { get; set; }

        // Canonical three-level catalog fields. Nullable for gradual migration of existing records.
        public string? PurchaseUnitName { get; set; }
        public string? MiddleUnitName { get; set; }
        public decimal? MiddleUnitsPerPurchaseUnit { get; set; }
        public string? SmallestUnitName { get; set; }
        public decimal? SmallestUnitsPerMiddleUnit { get; set; }
        public decimal? SmallestUnitWeightInKg { get; set; }
        public decimal? PricePerPurchaseUnit { get; set; }
        public decimal? PricePerMiddleUnit { get; set; }
        public decimal? PricePerSmallestUnit { get; set; }
    }
}
