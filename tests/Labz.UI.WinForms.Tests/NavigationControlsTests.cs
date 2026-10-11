using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class NavigationControlsTests
{
    [Fact]
    public void TabsKeepNativeSelectionSemantics()
    {
        RunSta(() =>
        {
            using var tabs = new LabzTabs();
            tabs.TabPages.Add("Overview");
            tabs.TabPages.Add("Details");
            tabs.SelectedIndex = 1;
            Assert.Equal(1, tabs.SelectedIndex);
            Assert.Equal("Details", tabs.SelectedTab?.Text);
            Assert.Equal(TabDrawMode.OwnerDrawFixed, tabs.DrawMode);
        });
    }

    [Fact]
    public void TooltipExtenderStoresPerControlText()
    {
        RunSta(() =>
        {
            using var tooltip = new LabzTooltip();
            using var button = new LabzButton();
            tooltip.SetLabzToolTip(button, "Save changes");
            Assert.Equal("Save changes", tooltip.GetLabzToolTip(button));
            Assert.True(tooltip.CanExtend(button));
        });
    }

    [Fact]
    public void ContextMenuAcceptsActions()
    {
        RunSta(() =>
        {
            using var menu = new LabzContextMenu();
            menu.Items.Add("Edit");
            Assert.Equal("Edit", menu.Items[0].Text);
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
