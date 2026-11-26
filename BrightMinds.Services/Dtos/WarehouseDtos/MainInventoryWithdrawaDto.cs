using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.WarehouseDtos
{
    public class MainInventoryWithdrawaDto : IValidatableObject
    {
        [Required]
        public string RecipientName { get; set; } = string.Empty;

        [MinLength(1, ErrorMessage = "لازم طلب السحب يكون فيه على الاقل صنف واحد")]
        public List<WithdrawRequestDto> WithdrawRequests { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var line in WithdrawRequests)
                foreach (var error in line.Validate(validationContext))
                    yield return error;
        }

        public class WithdrawRequestDto : IValidatableObject
        {
            [Required]
            public int Id { get; set; }

            [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
            public decimal? ContainerQuantity { get; set; }

            [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
            public decimal? Quantity { get; set; }

            public string? UnitLevel { get; set; }

            public int? RawMaterialUnitId { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Quantity.HasValue)
                {
                    if (RawMaterialUnitId.HasValue)
                    {
                        if (UnitLevel.HasValue)
                            yield return new ValidationResult("Use either RawMaterialUnitId or UnitLevel, not both.", new[] { nameof(RawMaterialUnitId), nameof(UnitLevel) });
                    }
                    else if (UnitLevel is not ("purchase" or "middle"))
                    {
                        yield return new ValidationResult("UnitLevel must be purchase or middle.", new[] { nameof(UnitLevel) });
                    }
                }
                else if (!ContainerQuantity.HasValue)
                {
                    yield return new ValidationResult("Provide Quantity or legacy ContainerQuantity.", new[] { nameof(Quantity), nameof(ContainerQuantity) });
                }

                if (Quantity.HasValue && ContainerQuantity.HasValue)
                    yield return new ValidationResult("Do not send Quantity and ContainerQuantity together.", new[] { nameof(Quantity), nameof(ContainerQuantity) });
            }
        }
    }
}
