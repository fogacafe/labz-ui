# LabzTabWorkspace

Document-style tabbed content host for WinForms. Pages are keyed by an
application-defined ID and are retained while switching tabs.

```csharp
var workspace = new LabzTabWorkspace { Dock = DockStyle.Fill };
workspace.Open("bonds/trades", "Trades", () => new TradesUserControl());
workspace.Open("funds/positions", "Positions", () => new PositionsUserControl());

// Reopening the same key only activates the existing page.
workspace.Open("bonds/trades", "Trades", () => new TradesUserControl());
workspace.Close("funds/positions");
```

- `Open`: lazily constructs content, reuses existing keys and activates the tab.
- `Activate`: shows the existing control without resetting state.
- `Close`: removes and disposes content, activates another open page if needed.
- `ActivePageChanged` and `PageClosed` expose lifecycle events.
- To support multiple instances of a page, use unique keys (e.g. `trades/123`).
- The host application decides whether to confirm closing dirty pages.

**Next:** wire `LabzAppShell.PageRequested` to `LabzTabWorkspace.Open`,
add keyboard shortcuts, overflow handling and a command palette.
