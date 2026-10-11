# Group header presenter

`LabzDataGridGroupHeader` is a reusable, themed group header for
host layouts. It exposes a title, arbitrary summary text, expansion
state and `ExpandToggleRequested` event.

```csharp
grid.GroupRows(row => ((TradeDto)row.DataBoundItem!).Counterparty);
foreach (var group in grid.GetGroups())
{
    var header = new LabzDataGridGroupHeader
    {
        GroupTitle = group.Key,
        GroupSummary = $"{group.Count} operations",
        Expanded = group.Expanded,
        Dock = DockStyle.Top
    };
    header.ExpandToggleRequested += (_, _) =>
    {
        header.Expanded = !header.Expanded;
        grid.SetGroupExpanded(group.Key, header.Expanded);
    };
    groupHeaderPanel.Controls.Add(header);
}
```

The host owns placement and aggregates (e.g., total notional).
This is **not yet an in-grid interleaved group row**: group headers
are separate WinForms controls. The presenter intentionally avoids
altering the DataGridView row model and its native edit/selection
behavior. Rebuild group metadata after sorting, filtering or rebinding.
