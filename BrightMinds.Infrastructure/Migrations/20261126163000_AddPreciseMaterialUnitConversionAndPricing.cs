using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrightMinds.Infrastructure.Migrations
{
    public partial class AddPreciseMaterialUnitConversionAndPricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ConversionFactor",
                table: "RawMaterialUnits",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<decimal>(name: "PricePerUnit", table: "RawMaterialUnits", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "WeightInKg", table: "RawMaterialUnits", type: "decimal(18,6)", precision: 18, scale: 6, nullable: true);
            migrationBuilder.CreateIndex(name: "IX_RawMaterialUnits_RawMaterialId_UnitId", table: "RawMaterialUnits", columns: new[] { "RawMaterialId", "UnitId" }, unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_RawMaterialUnits_RawMaterialId_UnitId", table: "RawMaterialUnits");
            migrationBuilder.DropColumn(name: "PricePerUnit", table: "RawMaterialUnits");
            migrationBuilder.DropColumn(name: "WeightInKg", table: "RawMaterialUnits");
            migrationBuilder.AlterColumn<float>(
                name: "ConversionFactor",
                table: "RawMaterialUnits",
                type: "real",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);
        }
    }
}
