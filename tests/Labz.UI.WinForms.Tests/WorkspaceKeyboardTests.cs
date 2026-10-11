using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class WorkspaceKeyboardTests
{
    [Fact]
    public void ShortcutsCycleTabsAndRespectCloseCancellation()
    {
        RunSta(() =>
        {
            using var workspace = new LabzTabWorkspace();
            workspace.Open("a", "A", () => new TextBox());
            workspace.Open("b", "B", () => new TextBox());
            workspace.Open("c", "C", () => new TextBox());

            Assert.True(workspace.HandleWorkspaceShortcut(Keys.Control | Keys.Tab));
            Assert.Equal("a", workspace.ActivePageId);
            Assert.True(workspace.HandleWorkspaceShortcut(Keys.Control | Keys.Shift | Keys.Tab));
            Assert.Equal("c", workspace.ActivePageId);

            EventHandler<LabzWorkspacePageClosingEventArgs> guard = (_, e) => e.Cancel = true;
            workspace.PageClosing += guard;
            Assert.False(workspace.HandleWorkspaceShortcut(Keys.Control | Keys.W));
            Assert.Equal("c", workspace.ActivePageId);
            Assert.Equal(3, workspace.OpenPageIds.Count);

            workspace.PageClosing -= guard;
            Assert.True(workspace.HandleWorkspaceShortcut(Keys.Control | Keys.W));
            Assert.Equal(2, workspace.OpenPageIds.Count);
            Assert.Equal("b", workspace.ActivePageId);
        });
    }

    [Fact]
    public void ShortcutsCanBeDisabled()
    {
        RunSta(() =>
        {
            using var workspace = new LabzTabWorkspace { KeyboardShortcutsEnabled = false };
            workspace.Open("a", "A", () => new Label());
            Assert.False(workspace.HandleWorkspaceShortcut(Keys.Control | Keys.W));
            Assert.Single(workspace.OpenPageIds);
            Assert.False(workspace.HandleWorkspaceShortcut(Keys.Control | Keys.F));
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
