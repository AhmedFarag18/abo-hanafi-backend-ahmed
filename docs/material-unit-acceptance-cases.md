# Material-unit backend acceptance cases

These cases are the required integration-test contract for the material-unit rollout. Run them against a disposable database after the endpoint/service implementation is wired to the additive unit-level schema.

## Recipe unit selection

- Create a recipe ingredient with `rawMaterialUnitId`; reload the recipe and verify the same ID, unit name, selected-unit price, and weight are returned.
- Reject a unit ID that belongs to a different raw material.
- For a unit with conversion factor `18`, price `8.33`, and weight `0.0556 kg`, quantity `5` resolves to inventory quantity `90`, ingredient cost `41.65`, and weight `0.278 kg`.
- Saving or editing a recipe does not modify warehouse inventory.

## Production

- Run production with enough inventory and verify each raw ingredient is deducted once in its inventory base unit.
- Submit a recipe whose ingredients collectively exceed stock and verify no inventory, product, or withdrawal-history rows are committed.
- Include a sub-recipe and verify its raw ingredients are included in the parent production plan and deducted once, without separately producing/deducting the sub-recipe as a second operation.
- Reject a repeated production request when the business operation is already completed/idempotency key is reused.

## Main-to-sub-warehouse withdrawal

- Accept explicit `unitLevel: "purchase"` and `unitLevel: "middle"` with positive `quantity`; reject any other level, including `smallest`.
- Verify fractional middle-unit withdrawals are converted to the inventory base unit and both warehouse balances and withdrawal history are updated atomically.
- Keep the legacy `containerQuantity` contract working during the client migration.

## Supply pricing

- Record the actual purchase/batch unit price on the supply item and inventory valuation.
- Verify creating/updating a supply does not silently mutate the raw-material catalog/reference prices.

## Repository regressions

- Verify deleting inventory rows for a supply removes matching rows and leaves unrelated rows unchanged.
- Verify missing main/sub warehouses return no inventory result rather than throwing from `First()`.
- Verify inventory merges reject incompatible units unless a registered conversion is applied.
