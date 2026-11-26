using BrightMinds.Core.Enums;
using BrightMinds.Core.Models;
using BrightMinds.Services.Services;

namespace BrightMinds.Tests;

public sealed class UnitConversionServiceTests
{
    private readonly UnitConversionService _conversion = new();

    [Theory]
    [InlineData(2500, UnitType.g, UnitType.kg, 2.5)]
    [InlineData(2.5, UnitType.kg, UnitType.g, 2500)]
    [InlineData(3, UnitType.kg, UnitType.kg, 3)]
    public void ConvertKnownUnitType_ConvertsOnlySupportedMassUnits(decimal quantity, UnitType source, UnitType target, decimal expected)
    {
        Assert.Equal(expected, _conversion.ConvertKnownUnitType(quantity, source, target));
    }

    [Fact]
    public void ConvertKnownUnitType_RejectsUnsupportedConversion()
    {
        Assert.Null(_conversion.ConvertKnownUnitType(2m, UnitType.piece, UnitType.kg));
    }

    [Fact]
    public void ConvertKnownUnitType_RejectsNegativeQuantity()
    {
        Assert.Null(_conversion.ConvertKnownUnitType(-1m, UnitType.kg, UnitType.kg));
    }

    [Fact]
    public void ToInventoryQuantity_UsesSelectedMaterialUnitFactor()
    {
        var material = new RawMaterial { Id = 42 };
        var unit = new RawMaterialUnit { Id = 131, RawMaterialId = 42, ConversionFactor = 18m };

        Assert.Equal(90m, _conversion.ToInventoryQuantity(material, 5m, unit.Id, unit));
    }

    [Fact]
    public void ToInventoryQuantity_RejectsUnitFromDifferentMaterial()
    {
        var material = new RawMaterial { Id = 42 };
        var unit = new RawMaterialUnit { Id = 131, RawMaterialId = 7, ConversionFactor = 18m };

        Assert.Null(_conversion.ToInventoryQuantity(material, 5m, unit.Id, unit));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ToInventoryQuantity_RejectsInvalidConversionFactor(decimal factor)
    {
        var material = new RawMaterial { Id = 42 };
        var unit = new RawMaterialUnit { Id = 131, RawMaterialId = 42, ConversionFactor = factor };

        Assert.Null(_conversion.ToInventoryQuantity(material, 1m, unit.Id, unit));
    }
}
