# DataGrid virtual mode

`LabzDataGrid` provides small helpers around the native WinForms
`DataGridView.VirtualMode` mechanism.

```csharp
grid.Columns.Add("Ticket", "Ticket");
grid.Columns.Add("Status", "Status");

grid.CellValueNeeded += (_, e) =>
{
    var item = pageCache.GetItem(e.RowIndex); // host-managed cache
    e.Value = e.ColumnIndex == 0 ? item.Ticket : item.Status;
};

grid.ConfigureVirtualRows(totalCount);
grid.UpdateVirtualRowCount(newTotalCount);
grid.ClearVirtualRows();
```

The host is responsible for asynchronous paging, caching, invalidation
and stable row indexing. Do not block `CellValueNeeded` on network
calls: return cached data immediately and fetch missing pages
asynchronously, then invalidate the affected rows on the UI thread.

Virtual mode is **not** server-side paging by itself. The grid does
not automatically fetch or retain any records. Configure columns and
subscribe to `CellValueNeeded` before setting the row count.

Bound `DataSource` and these unbound virtual row helpers are mutually
exclusive. For editable virtual grids, implement `CellValuePushed`
and appropriate value storage in the host.
