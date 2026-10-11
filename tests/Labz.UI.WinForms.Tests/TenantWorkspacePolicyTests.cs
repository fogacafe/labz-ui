using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class TenantWorkspacePolicyTests
{
    [Fact]
    public void DirtyPageVetoKeepsTenantAndAllTabs() => RunSta(() =>
    {
        using var shell = CreateShell(out var workspace);
        workspace.Open("a", "A", () => new Panel());
        workspace.Open("b", "B", () => new Panel());
        workspace.PageClosing += (_, e) => { if (e.PageId == "b") e.Cancel = true; };
        Assert.False(shell.StatusBar.TrySelectTenant("other"));
        Assert.Equal("current", shell.StatusBar.TenantContext?.Id);
        Assert.Equal(2, workspace.OpenPageIds.Count);
    });

    [Fact]
    public void ApprovedChangeClosesAllPages() => RunSta(() =>
    {
        using var shell = CreateShell(out var workspace);
        workspace.Open("a", "A", () => new Panel());
        workspace.Open("b", "B", () => new Panel());
        Assert.True(shell.StatusBar.TrySelectTenant("other"));
        Assert.Empty(workspace.OpenPageIds);
        Assert.Equal("other", shell.StatusBar.TenantContext?.Id);
    });

    private static LabzAppShell CreateShell(out LabzTabWorkspace workspace)
    {
        var shell = new LabzAppShell { ResetWorkspaceOnTenantChange = true };
        workspace = new LabzTabWorkspace();
        shell.ConnectWorkspace(workspace, (_, _) => new Panel());
        var current = new LabzTenantContext { Id = "current", DisplayName = "Current" };
        var other = new LabzTenantContext { Id = "other", DisplayName = "Other" };
        shell.StatusBar.SetAvailableTenants([current, other]);
        shell.StatusBar.TenantContext = current;
        return shell;
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
