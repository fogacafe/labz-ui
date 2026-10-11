using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class WorkspaceIntegrationTests
{
    [Fact]
    public void ShellOpensTabsAndReusesExistingPagesAcrossModules()
    {
        RunSta(() =>
        {
            using var shell = new LabzAppShell();
            var bonds = new LabzModule("bonds", "Public Bonds", "B");
            bonds.Pages.Add(new LabzNavigationPage("trades", "Trades"));
            var funds = new LabzModule("funds", "Funds", "F");
            funds.Pages.Add(new LabzNavigationPage("positions", "Positions"));
            shell.SetModules([bonds, funds]);
            var workspace = new LabzTabWorkspace();
            var calls = 0;
            shell.ConnectWorkspace(workspace, (_, _) =>
            {
                calls++;
                return new TextBox { Text = "Draft" };
            });

            Assert.True(shell.NavigateTo("bonds", "trades"));
            Assert.True(shell.NavigateTo("funds", "positions"));
            Assert.True(shell.NavigateTo("bonds", "trades"));
            Assert.Equal(2, calls);
            Assert.Equal(2, workspace.OpenPageIds.Count);
            Assert.Equal("bonds/trades", workspace.ActivePageId);
            Assert.Equal("bonds", shell.SelectedModuleId);
            Assert.True(workspace.Close("bonds/trades"));
            Assert.True(shell.NavigateTo("bonds", "trades"));
            Assert.Equal(3, calls);
        });
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
