using BrightMinds.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.WarehouseDtos
{
    /// <summary>New clients send Quantity plus UnitLevel or RawMaterialUnitId. Legacy ContainerQuantity is retained.</summary>
    public sealed class MainInventoryWithdrawaDto : IValidatableObject
    {
        [Required]
        public string RecipientName { get; set; } = string.Empty;

        [Required, MinLength(1, ErrorMessage = "لازم طلب السحب يكون فيه على الاقل صنف واحد")]
        public List<WithdrawRequestDto> WithdrawRequests { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var request in WithdrawRequests)
                foreach (var error in request.Validate(validationContext))
                    yield return error;
        }

        public sealed class WithdrawRequestDto : IValidatableObject
        {
            [Required]
            public int Id { get; set; }

            [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
            public decimal? Quantity { get; set; }

            /// <summary>Supported values are purchase or middle. Smallest-unit warehouse transfers are not supported.</summary>
            public string? UnitLevel { get; set; }

            public int? RawMaterialUnitId { get; set; }

            [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
            public decimal? ContainerQuantity { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (Quantity.HasValue && ContainerQuantity.HasValue)
                    yield return new ValidationResult("Send Quantity or ContainerQuantity, not both.", new[] { nameof(Quantity), nameof(ContainerQuantity) });

                if (Quantity.HasValue && RawMaterialUnitId.HasValue && !string.IsNullOrWhiteSpace(UnitLevel))
                    yield return new ValidationResult("Send either RawMaterialUnitId or UnitLevel, not both.", new[] { nameof(RawMaterialUnitId), nameof(UnitLevel) });

                if (Quantity.HasValue && !RawMaterialUnitId.HasValue && UnitLevel is not ("purchase" or "middle"))
                    yield return new ValidationResult("UnitLevel must be 'purchase' or 'middle'.", new[] { nameof(UnitLevel) });

                if (!Quantity.HasValue && !ContainerQuantity.HasValue)
                    yield return new ValidationResult("Quantity is required.", new[] { nameof(Quantity) });
            }
        }
    }
}
