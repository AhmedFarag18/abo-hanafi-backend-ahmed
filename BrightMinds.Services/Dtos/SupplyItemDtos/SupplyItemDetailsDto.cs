using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;

namespace BrightMinds.Services.Dtos.SupplyItemDtos
{
    public class SupplyItemDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ContainerQuantity { get; set; }
        public decimal Quantity { get; set; }
        public string DisplayDetails { get; set; } = string.Empty;

        // Actual batch price; separate from RawMaterial catalog/reference prices.
        public decimal UnitPrice { get; set; }
        public decimal? ValuationPrice { get; set; }
        public decimal TotalPrice { get; set; }

        public UnitType UnitType { get; set; }
        public int RawMaterialId { get; set; }
        public int SupplyId { get; set; }
        public decimal? ReceivedWeightKg { get; set; }
        public decimal? ExchangePrice { get; set; }
        public decimal? PriceMultiplier { get; set; }
        public ReceivingMethod ReceivedBy { get; set; }
        public decimal? OriginalQuantity { get; set; }
        public decimal? OriginalPrice { get; set; }
        public int? RawMaterialUnitId { get; set; }
    }
}
