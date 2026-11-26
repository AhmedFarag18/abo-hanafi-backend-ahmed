using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.RawMaterialDtos
{
    /// <summary>
    /// Additive raw-material contract. The legacy fields remain available for existing clients;
    /// the named levels and prices below are the preferred contract for new clients.
    /// </summary>
    public class RawMaterialDto : IValidatableObject
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "نوع التعامل مطلوب")]
        public HandlingType HandlingType { get; set; }

        // Legacy purchase/middle-unit fields (retained for compatibility).
        public string? ContainerUnitName { get; set; }
        [Range(0, double.MaxValue, ErrorMessage = "عدد الوحدات يجب ألا يكون أقل من صفر")]
        public decimal ContainerSize { get; set; }
        public UnitType UnitType { get; set; }
        public string? SubUnitName { get; set; }
        public decimal? UnitWeightInKg { get; set; }
        public decimal TotalContainerPrice { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public decimal NormalStockLevel { get; set; }
        public bool IsRestaurantSupply { get; set; }
        public bool IsMeat { get; set; }
        public decimal? MeatFactor { get; set; }
        public bool IsMultiplierLocked { get; set; }

        // Legacy smallest-unit fields (retained for compatibility).
        public string? InnerUnitName { get; set; }
        public decimal InnerUnitsPerSubUnit { get; set; } = 1;
        public decimal? InnerUnitWeightInKg { get; set; }

        // Canonical three-level contract. Levels are optional to support materials with 1–3 levels.
        public string? PurchaseUnitName { get; set; }
        public string? MiddleUnitName { get; set; }
        public decimal? MiddleUnitsPerPurchaseUnit { get; set; }
        public string? SmallestUnitName { get; set; }
        public decimal? SmallestUnitsPerMiddleUnit { get; set; }
        public decimal? SmallestUnitWeightInKg { get; set; }

        // These are catalog/reference prices entered by the user, not derived prices.
        public decimal? PricePerPurchaseUnit { get; set; }
        public decimal? PricePerMiddleUnit { get; set; }
        public decimal? PricePerSmallestUnit { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var purchaseName = PurchaseUnitName ?? ContainerUnitName;
            var middleName = MiddleUnitName ?? SubUnitName;
            var smallestName = SmallestUnitName ?? InnerUnitName;
            var middleFactor = MiddleUnitsPerPurchaseUnit ?? ContainerSize;
            var smallestFactor = SmallestUnitsPerMiddleUnit ?? InnerUnitsPerSubUnit;

            if (!string.IsNullOrWhiteSpace(MiddleUnitName) && string.IsNullOrWhiteSpace(purchaseName))
                yield return new ValidationResult("لا يمكن تحديد الوحدة الوسطى دون وحدة شراء", new[] { nameof(PurchaseUnitName) });
            if (!string.IsNullOrWhiteSpace(SmallestUnitName) && string.IsNullOrWhiteSpace(middleName))
                yield return new ValidationResult("لا يمكن تحديد الوحدة الصغرى دون وحدة وسطى", new[] { nameof(MiddleUnitName) });
            if (!string.IsNullOrWhiteSpace(MiddleUnitName) && middleFactor <= 0)
                yield return new ValidationResult("عدد الوحدات الوسطى في وحدة الشراء يجب أن يكون أكبر من صفر", new[] { nameof(MiddleUnitsPerPurchaseUnit) });
            if (!string.IsNullOrWhiteSpace(SmallestUnitName) && smallestFactor <= 0)
                yield return new ValidationResult("عدد الوحدات الصغرى في الوحدة الوسطى يجب أن يكون أكبر من صفر", new[] { nameof(SmallestUnitsPerMiddleUnit) });
            if (SmallestUnitWeightInKg.HasValue && SmallestUnitWeightInKg <= 0)
                yield return new ValidationResult("وزن الوحدة الصغرى يجب أن يكون أكبر من صفر", new[] { nameof(SmallestUnitWeightInKg) });

            if (PricePerPurchaseUnit.HasValue && PricePerPurchaseUnit <= 0)
                yield return new ValidationResult("سعر وحدة الشراء يجب أن يكون أكبر من صفر", new[] { nameof(PricePerPurchaseUnit) });
            if (PricePerMiddleUnit.HasValue && PricePerMiddleUnit <= 0)
                yield return new ValidationResult("سعر الوحدة الوسطى يجب أن يكون أكبر من صفر", new[] { nameof(PricePerMiddleUnit) });
            if (PricePerSmallestUnit.HasValue && PricePerSmallestUnit <= 0)
                yield return new ValidationResult("سعر الوحدة الصغرى يجب أن يكون أكبر من صفر", new[] { nameof(PricePerSmallestUnit) });

            if (!string.IsNullOrWhiteSpace(SmallestUnitName) && !SmallestUnitWeightInKg.HasValue &&
                HandlingType == HandlingType.CountAndWeight && !InnerUnitWeightInKg.HasValue)
                yield return new ValidationResult("وزن الوحدة الصغرى مطلوب لهذا النوع من المواد", new[] { nameof(SmallestUnitWeightInKg) });
        }
    }
}
