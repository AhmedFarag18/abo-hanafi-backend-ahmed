using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;

namespace BrightMinds.Core.Models
{
    public class SupplyItem : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal? ContainerQuantity { get; set; }
        public string? DisplayDetails { get; set; }

        // Actual batch/supply unit price. Catalog/reference prices remain on RawMaterial.
        public decimal UnitPrice { get; set; }

        // Optional explicit valuation price used to value inventory; defaults to UnitPrice in SupplyService.
        public decimal? ValuationPrice { get; set; }

        public int SupplyId { get; set; }
        public virtual Supply Supply { get; set; } = null!;
        public int RawMaterialId { get; set; }
        public virtual RawMaterial RawMaterial { get; set; } = null!;
        public int? RawMaterialUnitId { get; set; }
        public virtual RawMaterialUnit? RawMaterialUnit { get; set; }

        public UnitType UnitType { get; set; }
        public decimal? ReceivedWeightKg { get; set; }
        public decimal? ExchangePrice { get; set; }
        public decimal? PriceMultiplier { get; set; }
        public virtual ICollection<WarehouseSuppliesInventory> Warehouses { get; set; } = new List<WarehouseSuppliesInventory>();
        public ReceivingMethod ReceivedBy { get; set; } = ReceivingMethod.Container;
        public decimal? OriginalQuantity { get; set; }
        public decimal? OriginalPrice { get; set; }
    }
}
