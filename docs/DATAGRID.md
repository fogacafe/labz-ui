# LabzDataGrid

`LabzDataGrid` is a theme-aware `DataGridView`, not a replacement grid engine.
It preserves native WinForms `DataSource`, `BindingSource`, `BindingList<T>`,
column sort modes, keyboard navigation, selection events and Designer support.

Defaults:
- Full-row, single selection; configurable with `SelectionMode` and `MultiSelect`.
- Fill columns, striped rows, styled headers, theme-aware selection.
- No new-row or deletion UI by default; opt in using native properties.

```csharp
var grid = new LabzDataGrid { Dock = DockStyle.Fill };
grid.DataSource = new BindingSource { DataSource = orders };
grid.SelectionChanged += (_, _) => ShowSelectedOrder(grid.CurrentRow?.DataBoundItem);
```

**Sorting:** native `DataGridView` sorting works with supported sources,
including `DataTable` / `DataView` and sortable bindings. Plain
`BindingList<T>` does **not** automatically implement sortable list behavior;
use a sortable binding source or implement `IBindingList` sorting.

For very large datasets, consider native `VirtualMode`, paging, or
server-side sorting instead of materializing all rows in memory.
