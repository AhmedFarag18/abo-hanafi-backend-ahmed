using System.ComponentModel.DataAnnotations.Schema;

namespace BrightMinds.Core.Models
{
    /// <summary>
    /// A unit that belongs to one raw material. ConversionFactor is the number of
    /// inventory-base units represented by one unit of this type.
    /// </summary>
    public class RawMaterialUnit : BaseEntity
    {
        [ForeignKey(nameof(RawMaterial))]
        public int RawMaterialId { get; set; }
        public RawMaterial RawMaterial { get; set; } = null!;

        [ForeignKey(nameof(Unit))]
        public int UnitId { get; set; }
        public Unit Unit { get; set; } = null!;

        public decimal ConversionFactor { get; set; }
        public decimal? PricePerUnit { get; set; }
        public decimal? WeightInKg { get; set; }
    }
}
