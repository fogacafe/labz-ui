# DataGrid search

`TryFindNext(query)` selects the next matching cell in visible,
locally available rows and columns. Search wraps around after the last
cell, uses formatted cell values, and is case-insensitive by default.

```csharp
searchBox.TextChanged += (_, _) =>
{
    if (searchBox.TextLength > 0)
        grid.TryFindNext(searchBox.Text);
};

findNextButton.Click += (_, _) => grid.TryFindNext(searchBox.Text);
```

This is **navigation**, not filtering. It does not alter the
`BindingSource`, remove rows, or rebuild the grid.

Search is intentionally disabled for `VirtualMode` because scanning
all virtual rows can trigger expensive cache misses. For large datasets,
the host should search its provider/server and navigate to the matching
row index.

For frequent search over large locally bound lists, consider an indexed
search service rather than repeatedly scanning every cell.
