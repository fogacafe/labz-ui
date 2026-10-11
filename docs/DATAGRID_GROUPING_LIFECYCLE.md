# Grouping lifecycle

`GroupRows(selector)` stores the grouping selector and builds a
local group projection. `RefreshGrouping()` rebuilds it explicitly
when a row's grouping key changes. `GroupsChanged` lets a host
refresh its group header presenters.

The grid also refreshes grouping after native sorting and completed
data binding operations, while preserving expansion state by group key.

```csharp
grid.GroupRows(row => ((TradeDto)row.DataBoundItem!).Counterparty);
grid.GroupsChanged += (_, _) => RefreshGroupHeaderPanel(grid.GetGroups());

// When editing the counterparty or mutating items in place:
grid.RefreshGrouping();
```

Grouping remains a local, non-virtual feature. Group headers are
separate controls rather than inserted into the native grid row model.
For large/virtual datasets, use provider-side grouping.

Note: native DataGridView sorting and filtering have provider-specific
behavior; exercise the host's binding provider in integration tests.
