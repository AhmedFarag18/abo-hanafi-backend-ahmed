using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;

namespace BrightMinds.Services.IServices
{
    /// <summary>Converts a selected material-specific unit into the inventory quantity unit.</summary>
    public interface IUnitConversionService
    {
        decimal? ToInventoryQuantity(RawMaterial rawMaterial, decimal quantity, int? rawMaterialUnitId, RawMaterialUnit? selectedUnit);
        decimal? ConvertKnownUnitType(decimal quantity, UnitType source, UnitType target);
    }
}
