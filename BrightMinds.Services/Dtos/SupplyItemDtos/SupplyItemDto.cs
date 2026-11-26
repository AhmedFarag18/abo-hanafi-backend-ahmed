using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.SupplyItemDtos
{
    public class SupplyItemDto
    {
        [Required]
        public int RawMaterialId { get; set; }

        [Required]
        public ReceivingMethod ReceivedBy { get; set; } = ReceivingMethod.Container;

        // Batch/supply fields. These values never silently overwrite catalog/reference prices.
        public decimal? ContainerQuantity { get; set; }
        public decimal? ContainerPrice { get; set; }
        public decimal? ReceivedQuantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? ReceivedWeightKg { get; set; }
        public decimal? ExchangePrice { get; set; }
        public decimal? PriceMultiplier { get; set; }

        /// <summary>Optional explicit purchase/middle/smallest unit used for the batch line.</summary>
        public int? RawMaterialUnitId { get; set; }

        /// <summary>Optional valuation override. If omitted, the batch UnitPrice is used for inventory valuation.</summary>
        public decimal? ValuationPrice { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RawMaterialUnitId.HasValue)
            {
                if (ValuationPrice.HasValue && ValuationPrice.Value <= 0)
                    yield return new ValidationResult("ValuationPrice must be greater than zero.", new[] { nameof(ValuationPrice) });
            }
        }
    }
}
