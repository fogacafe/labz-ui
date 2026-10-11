# DataGrid extensibility

`LabzDataGrid` preserves native WinForms `CellFormatting`,
`RowPrePaint`, `CellPainting` and `CellValueNeeded` events.

For host-level composition without subclassing, optional callbacks
are available:

```csharp
grid.CellFormattingHandler = e =>
{
    if (grid.Columns[e.ColumnIndex].Name != "Status") return;
    if (e.Value is not string status) return;
    if (status == "ERROR")
        e.CellStyle.ForeColor = Color.Firebrick;
};

grid.RowPrePaintHandler = e =>
{
    // Customize row appearance if needed.
};
```

The callbacks run on the UI thread, in addition to the native events,
after their base event dispatch. Keep them fast; painting and formatting
may be called frequently during scrolling. For reusable specialized
controls, subclass `LabzDataGrid` and override native protected hooks.

The library intentionally does not impose business-domain status
semantics, repository access or network requests on the grid.
