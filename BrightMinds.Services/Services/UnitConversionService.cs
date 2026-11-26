using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;
using BrightMinds.Services.IServices;

namespace BrightMinds.Services.Services
{
    public sealed class UnitConversionService : IUnitConversionService
    {
        public decimal? ToInventoryQuantity(RawMaterial rawMaterial, decimal quantity, int? rawMaterialUnitId, RawMaterialUnit? selectedUnit)
        {
            if (quantity < 0 || rawMaterial == null)
                return null;
            if (rawMaterialUnitId.HasValue)
            {
                if (selectedUnit == null || selectedUnit.Id != rawMaterialUnitId.Value || selectedUnit.RawMaterialId != rawMaterial.Id || selectedUnit.ConversionFactor <= 0)
                    return null;
                return quantity * (decimal)selectedUnit.ConversionFactor;
            }
            // Legacy recipes express quantities in the material's inventory unit.
            return quantity;
        }

        public decimal? ConvertKnownUnitType(decimal quantity, UnitType source, UnitType target)
        {
            if (quantity < 0)
                return null;
            if (source == target)
                return quantity;
            if (source == UnitType.g && target == UnitType.kg)
                return quantity / 1000m;
            if (source == UnitType.kg && target == UnitType.g)
                return quantity * 1000m;
            return null;
        }
    }
}
