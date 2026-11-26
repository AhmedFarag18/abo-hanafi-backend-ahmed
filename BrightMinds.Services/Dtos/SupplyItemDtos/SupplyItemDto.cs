using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.SupplyItemDtos
{
    /// <summary>Batch receipt prices are independent of catalog/reference prices.</summary>
    public sealed class SupplyItemDto : IValidatableObject
    {
        [Required]
        public int RawMaterialId { get; set; }
        [Required]
        public ReceivingMethod ReceivedBy { get; set; } = ReceivingMethod.Container;

        public decimal? ContainerQuantity { get; set; }
        public decimal? ContainerPrice { get; set; }
        public decimal? ReceivedQuantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? ReceivedWeightKg { get; set; }
        public decimal? ExchangePrice { get; set; }
        public decimal? PriceMultiplier { get; set; }

        /// <summary>Selected material-specific unit for non-container receiving.</summary>
        public int? RawMaterialUnitId { get; set; }
        /// <summary>Optional explicit stock valuation rate; defaults to the actual batch rate.</summary>
        public decimal? ValuationPrice { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ValuationPrice.HasValue && ValuationPrice.Value <= 0)
                yield return new ValidationResult("ValuationPrice must be greater than zero.", new[] { nameof(ValuationPrice) });
            if (RawMaterialUnitId.HasValue && ReceivedBy == ReceivingMethod.Container)
                yield return new ValidationResult("Select a material unit only for quantity-based receiving.", new[] { nameof(RawMaterialUnitId) });
        }
    }
}
