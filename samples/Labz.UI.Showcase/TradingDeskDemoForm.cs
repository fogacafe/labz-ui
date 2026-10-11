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
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), AutoScroll = true };
        var heading = new LabzLabel
        {
            Text = (moduleId == "bonds" ? "Public Bonds" : "Funds") + " / " + pageId,
            Variant = LabzLabelVariant.Heading, Dock = DockStyle.Top, Height = 48
        };
        panel.Controls.Add(heading);

        if (pageId is "trades" or "positions" or "settlements")
        {
            var grid = new LabzDataGrid { Dock = DockStyle.Fill, ReadOnlyGrid = true };
            grid.DataSource = new BindingList<DemoTrade>([
                new DemoTrade(1001, "DRAFT", 125000m),
                new DemoTrade(1002, "RELEASED", 98000m),
                new DemoTrade(1003, "PENDING", 72000m)
            ]);
            panel.Controls.Add(grid);
            grid.BringToFront();
        }
        else
        {
            var input = new LabzTextBox
            {
                Dock = DockStyle.Top, Height = 54, Width = 340,
                PlaceholderText = pageId == "orders" ? "Enter instrument ticker" : "Paste import reference"
            };
            var note = new LabzLabel
            {
                Text = pageId == "orders" ? "Draft remains open until the tab is closed." : "Import preview (demonstration only).",
                Variant = LabzLabelVariant.Muted, Dock = DockStyle.Top, Height = 42
            };
            panel.Controls.Add(note);
            panel.Controls.Add(input);
            input.BringToFront();
        }
        heading.BringToFront();
        return panel;
    }

    private sealed record DemoTrade(int Ticket, string Status, decimal Amount);
}
