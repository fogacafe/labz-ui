# DataGrid grouping — foundation

The first grouping increment is a local group projection with group
keys, counts and expansion state. It does not yet draw dedicated group
header rows. A host can render group headers from `GetGroups()` and
supply its own text and aggregate values.

```csharp
grid.GroupRows(row => ((TradeDto)row.DataBoundItem!).Counterparty);
foreach (var group in grid.GetGroups())
{
    var title = $"{group.Key} | {group.Count} trades";
    // Render title in the host's group header component.
}
grid.SetGroupExpanded("XP", false);
grid.SetGroupExpanded("XP", true);
grid.ClearGrouping();
```

This is a deliberately limited foundation, **not** a full DevExpress-style
grouping view. It works only for currently bound non-virtual rows and
hides collapsed data rows. Call `GroupRows` again after sort, filter,
rebind or data changes. The host must manage any group header UI.

Future increments will provide a first-class group header presenter,
custom title/aggregates, expansion controls and data-change handling.
For large or virtual datasets, grouping belongs in the data provider.
