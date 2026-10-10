# Search actions and lookup direction

Both `LabzTextBox` and `LabzComboBox` support an optional search button.

```csharp
txtCounterparty.ShowSearchButton = true;
txtCounterparty.SearchRequested += async (_, _) =>
{
    // Open your own picker or call your API.
    // The control deliberately does not access any external data source.
    await OpenCounterpartyPickerAsync();
};

cmbCounterparty.ShowSearchButton = true;
cmbCounterparty.SearchRequested += (_, _) => OpenCounterpartyPicker();
```

The first version of `LabzComboBox` delegates item selection, data binding and keyboard
navigation to a native WinForms ComboBox. It does not yet implement a remote search dropdown.

## Planned LabzSearchLookup

Keep the richer remote lookup separate from the basic ComboBox. Proposed future API:
- asynchronous provider receiving search text and CancellationToken;
- configurable maximum result count (e.g. 10);
- optional columns for code, name and document;
- display and key selection independent of row shape;
- cancellation and debounce for in-flight requests.

These are design goals, **not implemented APIs**.
