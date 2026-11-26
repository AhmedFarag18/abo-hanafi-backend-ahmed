# Backend material-unit contract and verification status

The API supports material-specific unit references with `rawMaterialUnitId`. Recipe reads include the selected unit and associated unit metadata. Production should plan raw-material requirements recursively through sub-recipes, aggregate duplicate materials, validate all stock before writes, and deduct in a single transaction. Main-to-sub withdrawal accepts explicit `unitLevel` (`purchase` or `middle`) and legacy `containerQuantity` during migration.

## Supply pricing policy

- Raw-material catalog/reference prices belong to `RawMaterial` / `RawMaterialUnit` and are changed only by the raw-material APIs.
- `SupplyItem.UnitPrice` is the actual batch unit price.
- `SupplyItem.ValuationPrice` is the selected valuation price for inventory; when absent, the service uses the batch unit price.
- Creating or editing a supply must not update catalog price fields implicitly.

## Release gate before frontend integration

The following need build/test verification against a disposable database before the frontend switches to the new contracts:

1. `SupplyService` must stop mutating `RawMaterial.TotalContainerPrice` during create/update and persist `ValuationPrice` / `RawMaterialUnitId` on batch lines.
2. EF migration metadata/model snapshot must include `SupplyItem.ValuationPrice` and `SupplyItem.RawMaterialUnitId`; run `dotnet ef migrations script` and apply on a disposable copy.
3. Integration tests must cover insufficient-stock rollback, sub-recipe aggregation, selected-unit conversion/cost, duplicate production protection, explicit middle/purchase withdrawal, and catalog price immutability.
4. Verify the API startup resolves `MaterialWarehouseWorkflow` and all existing operation warehouse routes remain available.

Do not treat this document or an unbuilt commit as proof that the release gate passed.
