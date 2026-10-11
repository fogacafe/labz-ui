# Connect AppShell to Workspace

Use `ConnectWorkspace` to make sidebar page clicks open persistent document tabs.

```csharp
var shell = new LabzAppShell { Dock = DockStyle.Fill };
var bonds = new LabzModule("bonds", "Public Bonds", "B");
bonds.Pages.Add(new LabzNavigationPage("trades", "Trades", "Operations"));
shell.SetModules([bonds]);
shell.ConnectWorkspace(new LabzTabWorkspace(), (moduleId, pageId) =>
    pageId == "trades" ? new TradesControl() : new Panel());
form.Controls.Add(shell);
```

The workspace uses `moduleId/pageId` as the stable tab key. Navigating to
the same page again activates the existing control without resetting its state.
Switching modules does not close tabs. Closing a tab disposes its content.

`PageRequested` still fires after a successful open, for analytics or status UI.
The application owns its page factory and can choose different control types.

**Follow-up:** keyboard shortcuts, close veto for dirty pages, overflow handling,
and command palette.
