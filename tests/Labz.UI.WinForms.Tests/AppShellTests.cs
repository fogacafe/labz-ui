using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class AppShellTests
{
    [Fact]
    public void NavigationSelectsModuleAndRaisesPageRequest()
    {
        RunSta(() =>
        {
            using var shell = new LabzAppShell();
            var publicBonds = new LabzModule("public", "Public Bonds", "P");
            publicBonds.Pages.Add(new LabzNavigationPage("trades", "Trades", "Operations"));
            var funds = new LabzModule("funds", "Funds", "F");
            funds.Pages.Add(new LabzNavigationPage("positions", "Positions", "Queries"));
            shell.SetModules([publicBonds, funds]);
            Assert.Equal("public", shell.SelectedModuleId);
            LabzNavigationEventArgs? request = null;
            shell.PageRequested += (_, e) => request = e;
            Assert.True(shell.NavigateTo("funds", "positions"));
            Assert.Equal("funds", shell.SelectedModuleId);
            Assert.Equal("positions", request?.PageId);
            Assert.False(shell.NavigateTo("funds", "missing"));
            shell.SidebarExpanded = false;
            Assert.False(shell.SidebarExpanded);
        });
    }

    [Fact]
    public void RejectsDuplicateModuleIds()
    {
        RunSta(() =>
        {
            using var shell = new LabzAppShell();
            Assert.Throws<ArgumentException>(() => shell.SetModules([
                new LabzModule("x", "First"), new LabzModule("X", "Second")
            ]));
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
