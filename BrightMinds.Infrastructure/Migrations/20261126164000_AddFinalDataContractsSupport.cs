using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrightMinds.Infrastructure.Migrations
{
    public partial class AddFinalDataContractsSupport : Migration
    {
        // RawMaterialUnitId on RecipeIngrediants is created by AddThreeLevelRawMaterialUnits.
        // This migration is intentionally empty to keep its historical migration ID discoverable
        // without applying the same column/index/foreign-key a second time.
        protected override void Up(MigrationBuilder migrationBuilder) { }
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
