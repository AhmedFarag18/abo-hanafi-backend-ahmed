namespace BrightMinds.Api.Swagger
{
    /// <summary>
    /// OpenAPI examples for the material-unit APIs.
    /// </summary>
    public static class MaterialUnitSwaggerExamples
    {
        public const string RecipeIngredient = """
            {
              "rawMaterialId": 42,
              "rawMaterialUnitId": 131,
              "ingredientType": "RawMaterial",
              "quantity": 5,
              "unitType": "piece"
            }
            """;

        public const string MainWarehouseWithdrawal = """
            {
              "recipientName": "اسم المستلم",
              "withdrawRequests": [
                {
                  "id": 42,
                  "unitLevel": "middle",
                  "quantity": 10
                }
              ]
            }
            """;

        public const string RawMaterialCatalog = """
            {
              "name": "هوت دوج",
              "purchaseUnitName": "كرتونة",
              "middleUnitName": "كيلو",
              "middleUnitsPerPurchaseUnit": 10,
              "smallestUnitName": "صبع",
              "smallestUnitsPerMiddleUnit": 18,
              "pricePerPurchaseUnit": 1500,
              "pricePerMiddleUnit": 150,
              "pricePerSmallestUnit": 8.33,
              "smallestUnitWeightInKg": 0.0556
            }
            """;
    }
}
