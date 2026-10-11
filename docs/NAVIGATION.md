# Navigation controls

`LabzTabs` extends the native `TabControl`, preserving `TabPages`,
`SelectedIndex`, `SelectedIndexChanged` and Windows keyboard navigation.
Headers are owner-drawn using the active Labz theme.

`LabzTooltip` is a Designer-compatible extender component, backed by native
WinForms ToolTip with custom themed rendering.

`LabzContextMenu` extends ContextMenuStrip with a theme-aware renderer,
preserving menu items, keyboard navigation and context menu attachment.

```csharp
var tabs = new LabzTabs { Dock = DockStyle.Fill };
tabs.TabPages.Add(new TabPage("Orders"));
tabs.TabPages.Add(new TabPage("History"));

var tooltip = new LabzTooltip();
tooltip.SetLabzToolTip(tabs, "Switch between sections");

var menu = new LabzContextMenu();
menu.Items.Add("Refresh", null, (_, _) => RefreshOrders());
tabs.ContextMenuStrip = menu;
```

The Showcase includes all three. Validate Designer serialization, high DPI,
focus cues, disabled tab appearance and popup contrast visually on Windows.
