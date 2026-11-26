using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrightMinds.Infrastructure.Migrations
{
    public partial class AddBatchSupplyValuationAndSelectedUnit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValuationPrice",
                table: "SupplyItems",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RawMaterialUnitId",
                table: "SupplyItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyItems_RawMaterialUnitId",
                table: "SupplyItems",
                column: "RawMaterialUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyItems_RawMaterialUnits_RawMaterialUnitId",
                table: "SupplyItems",
                column: "RawMaterialUnitId",
                principalTable: "RawMaterialUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.Sql("UPDATE SupplyItems SET ValuationPrice = UnitPrice WHERE ValuationPrice IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_SupplyItems_RawMaterialUnits_RawMaterialUnitId", table: "SupplyItems");
            migrationBuilder.DropIndex(name: "IX_SupplyItems_RawMaterialUnitId", table: "SupplyItems");
            migrationBuilder.DropColumn(name: "RawMaterialUnitId", table: "SupplyItems");
            migrationBuilder.DropColumn(name: "ValuationPrice", table: "SupplyItems");
        }
    }
}
