using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;
using BrightMinds.Services.IServices;

namespace BrightMinds.Services.Services
{
    /// <summary>Converts a selected material-specific unit into the inventory base unit.</summary>
    public sealed class UnitConversionService : IUnitConversionService
    {
        public decimal? ToInventoryQuantity(RawMaterial rawMaterial, decimal quantity, int? rawMaterialUnitId, RawMaterialUnit? selectedUnit)
        {
            if (rawMaterial == null || quantity < 0)
                return null;

            if (!rawMaterialUnitId.HasValue)
                return quantity;

            if (selectedUnit == null || selectedUnit.Id != rawMaterialUnitId.Value ||
                selectedUnit.RawMaterialId != rawMaterial.Id || selectedUnit.ConversionFactor <= 0)
                return null;

            return quantity * selectedUnit.ConversionFactor;
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

        /// <summary>
        /// Converts purchase/middle/smallest-level quantities into the inventory base.
        /// The factors must be explicitly defined; no assumptions are made from unit names.
        /// </summary>
        public decimal? ToInventoryQuantity(RawMaterial material, decimal quantity, string unitLevel)
        {
            if (material == null || quantity < 0)
                return null;

            decimal factor;
            switch (unitLevel)
            {
                case "purchase":
                    factor = material.ContainerSize > 0 ? material.ContainerSize : material.MiddleUnitsPerPurchaseUnit ?? 0m;
                    break;
                case "middle":
                    var middles = material.MiddleUnitsPerPurchaseUnit.GetValueOrDefault();
                    var inventoryUnitsPerPurchase = material.ContainerSize > 0 ? material.ContainerSize : middles;
                    if (middles <= 0 || inventoryUnitsPerPurchase <= 0)
                        return null;
                    factor = inventoryUnitsPerPurchase / middles;
                    break;
                case "smallest":
                    var middleCount = material.MiddleUnitsPerPurchaseUnit.GetValueOrDefault();
                    var smallestPerMiddle = material.SmallestUnitsPerMiddleUnit.GetValueOrDefault();
                    var inventoryUnitsPerPurchaseUnit = material.ContainerSize > 0 ? material.ContainerSize : middleCount;
                    if (middleCount <= 0 || smallestPerMiddle <= 0 || inventoryUnitsPerPurchaseUnit <= 0)
                        return null;
                    factor = inventoryUnitsPerPurchaseUnit / (middleCount * smallestPerMiddle);
                    break;
                default:
                    return null;
            }

            return factor > 0 ? quantity * factor : null;
        }

        public decimal? GetPriceForLevel(RawMaterial material, string unitLevel)
        {
            if (material == null)
                return null;

            return unitLevel switch
            {
                "purchase" => material.PricePerPurchaseUnit,
                "middle" => material.PricePerMiddleUnit,
                "smallest" => material.PricePerSmallestUnit,
                _ => null
            };
        }

        public decimal? GetWeightForLevel(RawMaterial material, string unitLevel)
        {
            if (material == null)
                return null;

            return unitLevel switch
            {
                "smallest" => material.SmallestUnitWeightInKg ?? material.InnerUnitWeightInKg,
                "middle" => material.UnitWeightInKg,
                "purchase" when material.UnitWeightInKg.HasValue && material.ContainerSize > 0 => material.UnitWeightInKg.Value * material.ContainerSize,
                _ => null
            };
        }
    }
}
