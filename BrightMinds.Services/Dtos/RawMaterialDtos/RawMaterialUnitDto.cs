using System.ComponentModel.DataAnnotations;

namespace BrightMinds.Services.Dtos.RawMaterialDtos
{
    public sealed class RawMaterialUnitDto
    {
        public int? Id { get; set; }

        [Required]
        public int UnitId { get; set; }

        /// <summary>Inventory base units per selected unit.</summary>
        [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
        public decimal ConversionFactor { get; set; }

        [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
        public decimal? PricePerUnit { get; set; }

        [Range(typeof(decimal), "0.000001", "79228162514264337593543950335")]
        public decimal? WeightInKg { get; set; }
    }
}
