# DataGrid column selector

Right-click a column header to show the column visibility selector.
The menu follows the active Labz theme and lists columns in display
order. At least one column must remain visible.

```csharp
grid.ColumnSelectorEnabled = true; // default
// Optional programmatic menu, owned by the caller:
using var menu = grid.CreateColumnSelectorMenu();
menu.Show(grid, new Point(20, 20));
```

The selector works with the existing JSON layout profile API:
`ExportColumnLayout()` and `ImportColumnLayout(json)`.
Column names must remain stable across versions.

Set `ColumnSelectorEnabled = false` if the host application
implements a custom column management interface.
