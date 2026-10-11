using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridSearchTests
{
    [Fact]
    public void FindNextLocatesMatchingCellsAndWraps() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        grid.Rows.Add("Alpha");
        grid.Rows.Add("Bravo");
        grid.Rows.Add("Alpha Two");
        Assert.True(grid.TryFindNext("alpha"));
        Assert.Equal(0, grid.CurrentCell!.RowIndex);
        Assert.True(grid.TryFindNext("alpha"));
        Assert.Equal(2, grid.CurrentCell!.RowIndex);
        Assert.True(grid.TryFindNext("alpha"));
        Assert.Equal(0, grid.CurrentCell!.RowIndex);
        Assert.False(grid.TryFindNext("missing"));
    });

    [Fact]
    public void FindNextSkipsHiddenColumnsAndVirtualMode() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        grid.Columns.Add("Status", "Status");
        grid.Rows.Add("A", "Hidden");
        grid.Columns["Status"]!.Visible = false;
        Assert.False(grid.TryFindNext("Hidden"));
        grid.ConfigureVirtualRows(100);
        Assert.False(grid.TryFindNext("A"));
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
