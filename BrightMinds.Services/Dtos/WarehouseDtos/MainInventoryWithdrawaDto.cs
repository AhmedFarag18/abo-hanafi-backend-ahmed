using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.WarehouseDtos
{
    /// <summary>
    /// Transfers stock from the main warehouse. New clients should send UnitLevel and Quantity
    /// so the API can distinguish a purchase unit from a middle unit. ContainerQuantity remains
    /// available for older clients until the endpoint service is migrated.
    /// </summary>
    public class MainInventoryWithdrawaDto
    {
        [Required]
        public string RecipientName { get; set; } = string.Empty;

        [MinLength(1, ErrorMessage = "لازم طلب السحب يكون فيه على الاقل صنف واحد")]
        public List<WithdrawRequestDto> WithdrawRequests { get; set; } = new();

        public class WithdrawRequestDto : IValidatableObject
        {
            [Required]
            public int Id { get; set; }

            /// <summary>Legacy request field; use Quantity with UnitLevel in new clients.</summary>
            [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
            public decimal? ContainerQuantity { get; set; }

            /// <summary>Explicit unit selection: "purchase" or "middle". Smallest units are not accepted.</summary>
            public string? UnitLevel { get; set; }

            [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
            public decimal? Quantity { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Quantity.HasValue && UnitLevel is not ("purchase" or "middle"))
                    yield return new ValidationResult("UnitLevel must be 'purchase' or 'middle' when Quantity is provided.", new[] { nameof(UnitLevel) });

                if (!Quantity.HasValue && !ContainerQuantity.HasValue)
                    yield return new ValidationResult("Provide Quantity and UnitLevel, or the legacy ContainerQuantity.", new[] { nameof(Quantity), nameof(ContainerQuantity) });

                if (Quantity.HasValue && ContainerQuantity.HasValue)
                    yield return new ValidationResult("Do not send both the new Quantity and legacy ContainerQuantity fields.", new[] { nameof(Quantity), nameof(ContainerQuantity) });
            }
        }
    }
}
