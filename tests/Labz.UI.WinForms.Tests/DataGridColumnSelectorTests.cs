using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridColumnSelectorTests
{
    [Fact]
    public void SelectorTogglesColumnsAndKeepsOneVisible() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        grid.Columns.Add("Status", "Status");
        Assert.True(grid.ColumnSelectorEnabled);
        using var menu = grid.CreateColumnSelectorMenu();
        Assert.Equal(2, menu.Items.Count);
        ((ToolStripMenuItem)menu.Items[1]).PerformClick();
        Assert.False(grid.Columns["Status"]!.Visible);
        ((ToolStripMenuItem)menu.Items[0]).PerformClick();
        Assert.True(grid.Columns["Ticket"]!.Visible);
        ((ToolStripMenuItem)menu.Items[1]).PerformClick();
        Assert.True(grid.Columns["Status"]!.Visible);
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
