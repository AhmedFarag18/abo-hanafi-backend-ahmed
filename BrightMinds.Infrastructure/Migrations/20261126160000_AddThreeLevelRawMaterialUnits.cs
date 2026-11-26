using BrightMinds.Core.Models;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrightMinds.Infrastructure.Migrations
{
    /// <summary>Additive schema for the three-level raw-material catalog.</summary>
    public partial class AddThreeLevelRawMaterialUnits : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "PurchaseUnitName", table: "RawMaterials", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<string>(name: "MiddleUnitName", table: "RawMaterials", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "MiddleUnitsPerPurchaseUnit", table: "RawMaterials", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<string>(name: "SmallestUnitName", table: "RawMaterials", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "SmallestUnitsPerMiddleUnit", table: "RawMaterials", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "SmallestUnitWeightInKg", table: "RawMaterials", type: "decimal(18,6)", precision: 18, scale: 6, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "PricePerPurchaseUnit", table: "RawMaterials", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "PricePerMiddleUnit", table: "RawMaterials", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "PricePerSmallestUnit", table: "RawMaterials", type: "decimal(18,4)", precision: 18, scale: 4, nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PurchaseUnitName", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "MiddleUnitName", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "MiddleUnitsPerPurchaseUnit", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "SmallestUnitName", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "SmallestUnitsPerMiddleUnit", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "SmallestUnitWeightInKg", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "PricePerPurchaseUnit", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "PricePerMiddleUnit", table: "RawMaterials");
            migrationBuilder.DropColumn(name: "PricePerSmallestUnit", table: "RawMaterials");
        }
    }
}
