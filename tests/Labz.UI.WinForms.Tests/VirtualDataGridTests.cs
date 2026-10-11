using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class VirtualDataGridTests
{
    [Fact]
    public void VirtualRowsCanBeConfiguredResizedAndCleared() => RunSta(() =>
    {
        using var host = new Form();
        using var grid = new LabzDataGrid { Dock = DockStyle.Fill };
        grid.Columns.Add("Ticket", "Ticket");
        host.Controls.Add(grid);
        host.Show();
        grid.CellValueNeeded += (_, e) => e.Value = e.RowIndex + 1000;
        grid.ConfigureVirtualRows(100_000);
        Assert.True(grid.VirtualMode);
        Assert.Equal(100_000, grid.RowCount);
        Assert.Equal(1000, grid.Rows[0].Cells[0].Value);
        grid.UpdateVirtualRowCount(200);
        Assert.Equal(200, grid.RowCount);
        grid.ClearVirtualRows();
        Assert.False(grid.VirtualMode);
        Assert.Equal(0, grid.RowCount);
    });

    [Fact]
    public void VirtualRowsRejectBoundSourcesAndNegativeCounts() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        Assert.Throws<ArgumentOutOfRangeException>(() => grid.ConfigureVirtualRows(-1));
        grid.DataSource = new System.ComponentModel.BindingList<int>([1, 2]);
        Assert.Throws<InvalidOperationException>(() => grid.ConfigureVirtualRows(10));
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
