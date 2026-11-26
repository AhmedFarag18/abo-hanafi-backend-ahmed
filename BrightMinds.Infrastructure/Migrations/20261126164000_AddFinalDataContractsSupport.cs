using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrightMinds.Infrastructure.Migrations
{
    public partial class AddFinalDataContractsSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RawMaterialUnitId",
                table: "RecipeIngrediants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngrediants_RawMaterialUnitId",
                table: "RecipeIngrediants",
                column: "RawMaterialUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngrediants_RawMaterialUnits_RawMaterialUnitId",
                table: "RecipeIngrediants",
                column: "RawMaterialUnitId",
                principalTable: "RawMaterialUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngrediants_RawMaterialUnits_RawMaterialUnitId",
                table: "RecipeIngrediants");

            migrationBuilder.DropIndex(
                name: "IX_RecipeIngrediants_RawMaterialUnitId",
                table: "RecipeIngrediants");

            migrationBuilder.DropColumn(
                name: "RawMaterialUnitId",
                table: "RecipeIngrediants");
        }
    }
}
