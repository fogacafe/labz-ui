using System.ComponentModel;
using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;
using Labz.UI.WinForms.Dialogs;
using Labz.UI.WinForms.Navigation;

namespace Labz.UI.Showcase;

/// <summary>End-to-end demo of a multi-module, multi-tenant desktop trading application.</summary>
public sealed class TradingDeskDemoForm : Form
{
    private readonly LabzAppShell _shell = new() { Dock = DockStyle.Fill };
    private readonly LabzTabWorkspace _workspace = new();

    public TradingDeskDemoForm()
    {
        Text = "Labz UI • Trading Desk Demo";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 600);
        ClientSize = new Size(1240, 780);
        Controls.Add(_shell);

        var bonds = new LabzModule("bonds", "Public Bonds", "B");
        bonds.Pages.Add(new LabzNavigationPage("orders", "Order Entry", "Trading"));
        bonds.Pages.Add(new LabzNavigationPage("trades", "Trade Blotter", "Trading"));
        bonds.Pages.Add(new LabzNavigationPage("import", "Bulk Import", "Operations"));
        var funds = new LabzModule("funds", "Funds", "F");
        funds.Pages.Add(new LabzNavigationPage("positions", "Positions", "Portfolio"));
        funds.Pages.Add(new LabzNavigationPage("settlements", "Settlements", "Operations"));
        _shell.SetModules([bonds, funds]);
        _shell.ConnectWorkspace(_workspace, CreatePage);
        _shell.ResetWorkspaceOnTenantChange = true;
        _shell.StatusBar.UserContext = new LabzUserContext { Id = "demo", DisplayName = "Demo User" };
        var deskA = new LabzTenantContext { Id = "desk-a", DisplayName = "Trading Desk" };
        var deskB = new LabzTenantContext { Id = "desk-b", DisplayName = "Research Desk" };
        _shell.StatusBar.TenantContext = deskA;
        _shell.StatusBar.SetAvailableTenants([deskA, deskB]);
        _shell.StatusBar.TenantSelectorEnabled = true;
        _shell.StatusBar.EnvironmentName = "DEV";
        _shell.StatusBar.StatusText = "Ctrl+K: find pages • Ctrl+Tab: switch tabs";
        _shell.StatusBar.TenantSwitchHandler = async (_, _, ct) =>
        {
            await Task.Delay(350, ct); // Simulated application session switch
            return true;
        };
        _shell.StatusBar.TenantSwitchFailed += (_, e) =>
            LabzMessageBox.Show(e.Exception.Message, "Tenant switch failed", LabzDialogKind.Error, owner: this);
        _shell.StatusBar.UserRequested += (_, _) =>
            LabzMessageBox.Show("User management belongs to the host application.", "User", owner: this);
        _workspace.PageClosing += (_, e) =>
        {
            if (e.PageId != "bonds/orders") return;
            if (LabzMessageBox.Confirm("Discard the order entry draft?", "Unsaved changes", this) != DialogResult.Yes)
                e.Cancel = true;
        };
        _shell.NavigateTo("bonds", "trades");
    }

    private static Control CreatePage(string moduleId, string pageId)
    {
        var theme = UiThemeManager.Current;
        var moduleTitle = moduleId == "bonds" ? "Public Bonds" : "Funds";
        var pageTitle = pageId switch
        {
            "trades" => "Trade Blotter",
            "orders" => "Order Entry",
            "import" => "Bulk Import",
            "positions" => "Positions",
            "settlements" => "Settlements",
            _ => pageId
        };

        // Separate the page chrome from the content: the header never overlaps a docked grid.
        var page = new Panel { Dock = DockStyle.Fill, BackColor = theme.Background, Padding = new Padding(24, 20, 24, 24) };
        var header = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = theme.Background };
        var breadcrumb = new Label
        {
            Text = moduleTitle.ToUpperInvariant() + "  /  " + pageTitle.ToUpperInvariant(),
            Dock = DockStyle.Top, Height = 23, ForeColor = theme.MutedForeground,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f, FontStyle.Bold)
        };
        var title = new Label
        {
            Text = pageTitle, Dock = DockStyle.Top, Height = 39,
            ForeColor = theme.Foreground, Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 19f, FontStyle.Bold)
        };
        header.Controls.Add(title);
        header.Controls.Add(breadcrumb);

        var surface = new Panel
        {
            Dock = DockStyle.Fill, BackColor = theme.Surface,
            Padding = new Padding(1)
        };
        page.Controls.Add(surface);
        page.Controls.Add(header);

        if (pageId is "trades" or "positions" or "settlements")
        {
            var grid = new LabzDataGrid { Dock = DockStyle.Fill, ReadOnlyGrid = true };
            grid.DataSource = new BindingList<DemoTrade>([
                new DemoTrade(1001, "DRAFT", 125000m),
                new DemoTrade(1002, "RELEASED", 98000m),
                new DemoTrade(1003, "PENDING", 72000m)
            ]);
            surface.Controls.Add(grid);
        }
        else
        {
            // Form pages use a compact section; data pages retain a full-size grid.
            // The host canvas remains full-size, but the form surface does not.
            surface.BackColor = theme.Background;
            surface.Padding = new Padding(0);
            var section = new Panel
            {
                Dock = DockStyle.None,
                Location = Point.Empty,
                Height = 168,
                BackColor = theme.Surface,
                Padding = new Padding(20)
            };
            // A docked panel combined with MaximumSize could collapse to zero height.
            // Explicit bounds keep the card visible while limiting its width.
            void LayoutSection()
            {
                if (section.IsDisposed || surface.IsDisposed) return;
                section.SetBounds(0, 0, Math.Max(1, Math.Min(760, surface.ClientSize.Width)), 168);
            }
            surface.Resize += (_, _) => LayoutSection();
            var sectionTitle = new Label
            {
                Text = pageId == "orders" ? "Instrument details" : "Import source",
                Dock = DockStyle.Top, Height = 30,
                ForeColor = theme.Foreground,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11f, FontStyle.Bold)
            };
            var input = new LabzTextBox
            {
                Dock = DockStyle.Top, Height = 54,
                PlaceholderText = pageId == "orders" ? "Enter instrument ticker" : "Paste import reference"
            };
            var note = new LabzLabel
            {
                Text = pageId == "orders" ? "Draft remains open until the tab is closed." : "Import preview (demonstration only).",
                Variant = LabzLabelVariant.Muted, Dock = DockStyle.Top, Height = 32
            };
            // Reverse docking insertion order so the title appears above the input.
            section.Controls.Add(note);
            section.Controls.Add(input);
            section.Controls.Add(sectionTitle);
            surface.Controls.Add(section);
            LayoutSection();
        }
        return page;
    }

    private sealed record DemoTrade(int Ticket, string Status, decimal Amount);
}
