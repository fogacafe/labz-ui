using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class WorkspaceCloseGuardTests
{
    [Fact]
    public void CancelledClosePreservesPageAndActiveSelection()
    {
        RunSta(() =>
        {
            using var workspace = new LabzTabWorkspace();
            var draft = new TextBox { Text = "Unsaved trade" };
            workspace.Open("trades", "Trades", () => draft);
            var closed = 0;
            workspace.PageClosed += (_, _) => closed++;
            EventHandler<LabzWorkspacePageClosingEventArgs> guard = (_, args) =>
            {
                if (args.PageId == "trades") args.Cancel = true;
            };
            workspace.PageClosing += guard;

            Assert.False(workspace.Close("trades"));
            Assert.Equal("trades", workspace.ActivePageId);
            Assert.Contains("trades", workspace.OpenPageIds);
            Assert.False(draft.IsDisposed);
            Assert.Equal(0, closed);

            workspace.PageClosing -= guard;
            Assert.True(workspace.Close("trades"));
            Assert.True(draft.IsDisposed);
            Assert.Equal(1, closed);
        });
    }

    [Fact]
    public void CloseGuardReceivesPageMetadata()
    {
        RunSta(() =>
        {
            using var workspace = new LabzTabWorkspace();
            workspace.Open("funds/positions", "Positions", () => new Label());
            string? id = null;
            string? title = null;
            workspace.PageClosing += (_, args) => { id = args.PageId; title = args.Title; };
            Assert.True(workspace.Close("funds/positions"));
            Assert.Equal("funds/positions", id);
            Assert.Equal("Positions", title);
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
