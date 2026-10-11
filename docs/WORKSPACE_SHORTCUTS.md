# Workspace keyboard shortcuts

`LabzTabWorkspace` supports:

| Shortcut | Action |
| --- | --- |
| Ctrl+Tab | Activate next tab (wraps around) |
| Ctrl+Shift+Tab | Activate previous tab |
| Ctrl+W | Request close of active tab |

Close requests use the existing cancelable `PageClosing` event. If the
application rejects closing a dirty page, the tab remains open.

Shortcuts are enabled by default and can be disabled with
`KeyboardShortcutsEnabled = false`. They are handled through WinForms
`ProcessCmdKey` when the workspace receives command-key routing.

If the hosting Form handles shortcuts first, it can delegate explicitly:

```csharp
protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
{
    if (workspace.HandleWorkspaceShortcut(keyData)) return true;
    return base.ProcessCmdKey(ref msg, keyData);
}
```

This PR does not introduce global shortcuts or a command palette.
