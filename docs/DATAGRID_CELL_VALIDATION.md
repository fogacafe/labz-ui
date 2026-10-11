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

## Row-level validation and manual control

```csharp
grid.CellValueChangedHandler = e =>
{
    if (e.RowIndex < 0) return;
    grid.SetRowValidation(
        e.RowIndex,
        LabzDataGrid.CellValidationSeverity.Warning,
        "Changed since last validation; revalidate before saving");
};

// Once the host validates again:
grid.ClearRowValidation(rowIndex);

// Optional host-defined colors, without built-in validation markers:
grid.CustomCellStyleHandler = e =>
{
    if (e.RowIndex >= 0 && myInvalidRows.Contains(e.RowIndex))
        e.CellStyle.BackColor = Color.MistyRose;
};
```

Native `CellValueChanged`, `CellValidating`, `CellFormatting`,
`CellPainting` and `RowPrePaint` remain available. For checkbox
cells, consider `CurrentCellDirtyStateChanged` with
`CommitEdit(DataGridViewDataErrorContexts.Commit)` to receive
changes immediately.

Row validation markers show an underline and tooltip across all cells
in the row. A cell-specific marker takes precedence over a row marker.
The host controls whether a change invalidates a cell or the entire
row; the library does not automatically infer business rules.

As with cell markers, row markers use positional indices and should
be rebuilt after sorting, filtering or rebinding.
