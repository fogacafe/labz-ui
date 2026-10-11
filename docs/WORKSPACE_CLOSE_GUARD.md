# Cancelable workspace page closing

Subscribe to `LabzTabWorkspace.PageClosing` to prevent accidental loss of
unsaved changes. The event fires **before** content is removed or disposed.

```csharp
workspace.PageClosing += (_, e) =>
{
    if (!IsDirty(e.PageId)) return;
    e.Cancel = MessageBox.Show(
        "Discard unsaved changes?", "Confirm close",
        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes;
};
```

When `Cancel = true`, `Close(id)` returns false, the page stays open,
the active tab does not change, and `PageClosed` is not raised.
When the close is permitted, content is disposed and `PageClosed` fires.
No dialog is shown by default; applications own their dirty-state rules.
