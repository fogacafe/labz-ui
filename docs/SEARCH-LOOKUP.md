# LabzSearchLookup

A WinForms lookup that accepts a consumer-owned asynchronous search provider.

```csharp
var lookup = new LabzSearchLookup
{
    MaxResults = 10,
    DebounceMilliseconds = 350,
    Columns =
    [
        new LabzLookupColumn("code", "Code"),
        new LabzLookupColumn("name", "Name"),
        new LabzLookupColumn("document", "Document")
    ]
};

lookup.SearchProvider = async (query, limit, ct) =>
{
    var rows = await api.SearchCounterpartiesAsync(query, limit, ct);
    return rows.Select(row => new LabzLookupItem(
        row.Id, row.Name,
        new Dictionary<string, object?>
        {
            ["code"] = row.Code,
            ["name"] = row.Name,
            ["document"] = row.Document
        })).ToArray();
};

lookup.SelectionChanged += (_, _) =>
{
    var selectedKey = lookup.SelectedItem?.Key;
};
lookup.SearchFailed += (_, e) => ShowError(e.Exception.Message);
```

Typing debounces searches; Enter and the search button trigger immediate search.
The provider receives the current query, maximum result count and cancellation token.
A new query cancels the previous request; stale successful results are ignored.
Double-click or Enter on a result selects it. Optional columns render in a native grid.
The results grid is hosted in a floating popup, so the control occupies only one input row.\nThe popup shows searching, no-results and failure states. Escape closes it; Down enters\nthe results, and Enter or double-click selects a row. A full keyboard accessibility\naudit and Designer polish are still future work.
