# Labz App Shell

`LabzAppShell` provides a VS Code-inspired activity bar, grouped module sidebar,
and a neutral content host for WinForms applications. It deliberately does not
manage document tabs or create pages in this PR.

```csharp
using Labz.UI.WinForms.Navigation;

var shell = new LabzAppShell { Dock = DockStyle.Fill };
var bonds = new LabzModule("bonds", "Public Bonds", "B");
bonds.Pages.Add(new LabzNavigationPage("tickets", "Ticket entry", "Operations"));
bonds.Pages.Add(new LabzNavigationPage("imports", "Import", "Operations"));
shell.SetModules([bonds]);
shell.PageRequested += (_, args) =>
{
    // Host app decides how to open or activate a UserControl.
    Console.WriteLine($"{args.ModuleId}/{args.PageId}");
};
form.Controls.Add(shell);
```

The activity bar uses short text glyphs for now; icons, tooltips and navigation
polish can be refined without affecting the module/page model. Page requests are
events; switching modules does not close existing content. Set
`SidebarExpanded = false` to reclaim horizontal space.

**Next:** workspace tabs, page lifetime management, command palette and detachable windows.
