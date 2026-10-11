# Editable grid and cell validation decorations

The grid defaults to read-only. Opt into native WinForms editing:

```csharp
grid.EditableGrid = true;
grid.SetCellValidation(
    rowIndex, columnIndex,
    LabzDataGrid.CellValidationSeverity.Error,
    "Invalid ticket");
grid.SetCellValidation(
    rowIndex, anotherColumnIndex,
    LabzDataGrid.CellValidationSeverity.Warning,
    "Check amount");
grid.ClearCellValidation(rowIndex, columnIndex);
grid.ClearCellValidations();
```

Validation decorations show a colored underline (red for errors,
amber for warnings) and a tooltip. They do not change data, prevent
commits or perform business validation. The host should use native
`CellValidating`, `CellEndEdit`, `DataError` and binding mechanisms
to enforce rules and update markers.

Markers are indexed by row and column position. **Clear and rebuild
markers after sorting, filtering, rebinding or changing row counts**,
or they may refer to different records. A future revision may support
stable business keys for durable markers.
