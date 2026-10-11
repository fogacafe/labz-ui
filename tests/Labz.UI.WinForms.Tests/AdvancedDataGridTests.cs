using System.ComponentModel;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class AdvancedDataGridTests
{
    [Fact]
    public void MultiSelectionAndSelectedItemsUseBoundRows() => RunSta(() =>
    {
        using var grid = new LabzDataGrid { MultiRowSelection = true };
        using var host = new Form();
        host.Controls.Add(grid);
        host.Show();
        grid.DataSource = new BindingList<Sample>([new(1), new(2), new(3)]);
        Application.DoEvents();
        Assert.Equal(3, grid.Rows.Count);
        grid.ClearSelection();
        grid.Rows[0].Selected = true;
        grid.Rows[2].Selected = true;
        Assert.Equal([1, 3], grid.GetSelectedItems<Sample>().Select(x => x.Id).ToArray());
    });

    [Fact]
    public void UnsupportedFilterDoesNotModifyGrid() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        using var host = new Form();
        host.Controls.Add(grid);
        host.Show();
        grid.DataSource = new BindingList<Sample>([new(1), new(2)]);
        Application.DoEvents();
        Assert.Equal(2, grid.Rows.Count);
        Assert.False(grid.TryApplyFilter("Id = 1"));
        Assert.False(grid.TryClearFilter());
        Assert.Equal(2, grid.Rows.Count);
    });

    private sealed record Sample(int Id);

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
