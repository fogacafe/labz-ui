# Global command palette

`LabzAppShell` supports **Ctrl+K** to find any registered page across modules.
Use `shell.ShowCommandPalette()` to open it programmatically, or set
`shell.CommandPaletteEnabled = false` to disable it.

- Search is case-insensitive and matches module title, page title and group.
- Multiple space-separated terms must all match.
- Up/Down selects a result; Enter navigates; Escape dismisses.
- Navigation reuses the existing workspace tab when the shell is connected.
- The palette is deliberately a simple modal dialog in this version; it can
  later become a borderless floating overlay with ranking and command actions.

```csharp
shell.SetModules([bondsModule, fundsModule]);
shell.ConnectWorkspace(new LabzTabWorkspace(), CreatePage);
shell.ShowCommandPalette();
```
