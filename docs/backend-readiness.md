# Backend readiness: current status and remaining gates

This checklist is intentionally conservative: repository changes are not considered verified until a build and tests run in CI or a local .NET environment.

## Implemented scaffolding

- Three-level raw-material contract fields and manual catalog price fields exist in DTO/model code.
- Recipe ingredient model/DTO has `RawMaterialUnitId`; recipe loading includes raw-material units.
- Material warehouse workflow and explicit purchase/middle withdrawal request fields have been introduced.
- Batch supply valuation fields and an additive migration have been introduced.
- Unit-conversion unit tests are added in `BrightMinds.Tests`.

## Blocking issues to resolve before frontend workflow integration

- `SupplyService.CreateAsync` and `UpdateAsync` still mutate `RawMaterial.TotalContainerPrice` and do not consistently persist selected unit / valuation price; fix both paths.
- `MaterialWarehouseWorkflow` needs fixes for source-stock unit conversion, merged transfer accounting, and duplicate production/idempotency. Validate behavior under a real database transaction.
- `RawMaterialService` still recalculates legacy unit prices and needs to preserve manual catalog prices on create, update, and reads.
- Additive migration classes need discoverable EF migration metadata and the model snapshot needs to match `SupplyItem`/recipe-unit entity configuration. Check for duplicate `RawMaterialUnitId` migration operations before applying.
- Add integration tests for recipe save-without-stock-change, full rollback on insufficient stock, nested recipes, supply catalog-price immutability, and main/sub stock transfers.
- Run `dotnet test` and generate/apply migration scripts on a disposable database before treating the backend as ready.

Frontend work may begin against DTO shapes in parallel, but production, withdrawal, and supply screens should not rely on these workflows until the blocking checks pass.
