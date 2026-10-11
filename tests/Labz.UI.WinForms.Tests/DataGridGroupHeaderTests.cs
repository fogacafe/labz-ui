using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridGroupHeaderTests
{
    [Fact]
    public void HeaderExposesTitleSummaryAndExpansion() => RunSta(() =>
    {
        using var header = new LabzDataGridGroupHeader
        {
            GroupTitle = "XP",
            GroupSummary = "128 trades · R$ 12.4m",
            Expanded = false
        };
        Assert.Equal("XP", header.GroupTitle);
        Assert.Contains("128", header.GroupSummary);
        Assert.False(header.Expanded);
        header.Expanded = true;
        Assert.True(header.Expanded);
    });

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
