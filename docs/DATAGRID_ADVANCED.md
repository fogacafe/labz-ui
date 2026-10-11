# DataGrid selection and filtering

`LabzDataGrid` builds on the native `DataGridView` APIs rather than
introducing a second data-binding engine.

```csharp
grid.MultiRowSelection = true;
IReadOnlyList<Trade> selected = grid.GetSelectedItems<Trade>();
```

For provider-backed filtering, bind through a `BindingSource` whose
underlying list supports filtering (such as `DataView`):

```csharp
var source = new BindingSource { DataSource = dataTable.DefaultView };
grid.DataSource = source;
grid.TryApplyFilter("Status = 'DRAFT'");
grid.TryClearFilter();
```

A plain `BindingList<T>` does not implement filtering; in that case
`TryApplyFilter` returns false and does not alter the grid.

For large remote datasets, use a host-managed paged or virtual-mode
data source; these helpers do not load, filter or paginate records on
the server. Existing native context menus, cell editing and validation
events remain available.

This iteration also disposes replaced header fonts to avoid leaking
GDI resources during repeated theme/font updates.
