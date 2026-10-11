# Stable-key validation

Use record identities rather than visual row indices when sorting, filtering
or rebinding can move records.

```csharp
grid.ValidationRowKeySelector = row =>
    (row.DataBoundItem as TradeDto)?.Id.ToString();

grid.SetKeyedRowValidation(
    tradeId.ToString(),
    LabzDataGrid.CellValidationSeverity.Warning,
    "Operation changed; revalidation required");

grid.SetKeyedCellValidation(
    tradeId.ToString(), "Quantity",
    LabzDataGrid.CellValidationSeverity.Error,
    "Quantity must be positive");

grid.ClearKeyedRowValidation(tradeId.ToString());
grid.ClearKeyedCellValidation(tradeId.ToString(), "Quantity");
grid.ClearKeyedValidations();
```

Use unique, stable keys and stable `DataGridViewColumn.Name` values.
Keyed markers take precedence over positional markers and persist in
the grid until cleared. The application controls their lifecycle when
switching datasets, tenants or users.

The key selector is evaluated for displayed cells; keep it cheap and
side-effect free. Keyed validation is for bound, non-virtual grids.
Virtual mode needs a separate provider-aware key mapping.

The existing positional `SetRowValidation` and `SetCellValidation`
APIs remain available for simpler scenarios.
