# DataGrid layout profiles

Users can reorder columns by dragging headers. The grid exposes
`ColumnReorderingEnabled` to toggle this behavior.

Export and restore order, width and visibility with JSON:

```csharp
var json = grid.ExportColumnLayout();
await userSettings.SaveAsync("trades/blotter", json);

// Once the grid columns have been created:
grid.ImportColumnLayout(await userSettings.LoadAsync("trades/blotter"));
```

The library does **not** store layouts or access databases. The host
chooses profile names, users, screens and persistence.

Column identity is based on `DataGridViewColumn.Name`. Give every
column a stable unique name. Removed/unknown columns are ignored on
restore; invalid widths are ignored. Do not pass untrusted arbitrary
JSON without size limits and error handling.

Future increments: column selector UI, search, validation decorations,
editing helpers and grouping.
