using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class TabWorkspaceTests
{
    [Fact]
    public void ReopeningPageActivatesExistingContentWithoutCallingFactory()
    {
        RunSta(() =>
        {
            using var workspace = new LabzTabWorkspace();
            var count = 0;
            workspace.Open("bonds/trades", "Trades", () => { count++; return new TextBox { Text = "Draft" }; });
            workspace.Open("funds/positions", "Positions", () => new Label());
            workspace.Open("bonds/trades", "Trades", () => { count++; return new Label(); });
            Assert.Equal(1, count);
            Assert.Equal("bonds/trades", workspace.ActivePageId);
            Assert.Equal(2, workspace.OpenPageIds.Count);
        });
    }

    [Fact]
    public void ClosingActivePageDisposesContentAndSelectsAnother()
    {
        RunSta(() =>
        {
            using var workspace = new LabzTabWorkspace();
            var first = new TextBox();
            workspace.Open("first", "First", () => first);
            workspace.Open("second", "Second", () => new Label());
            Assert.True(workspace.Close("second"));
            Assert.Equal("first", workspace.ActivePageId);
            Assert.True(workspace.Close("first"));
            Assert.True(first.IsDisposed);
            Assert.Null(workspace.ActivePageId);
            Assert.False(workspace.Close("missing"));
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
