using System.ComponentModel;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridTests
{
    [Fact]
    public void GridSupportsNativeBindingAndFullRowSelection()
    {
        RunSta(() =>
        {
            using var grid = new LabzDataGrid();
            var rows = new BindingList<GridItem>
            {
                new(1, "Draft"), new(2, "Released")
            };
            grid.Bind(rows);
            Assert.Same(rows, grid.DataSource);
            Assert.Equal(DataGridViewSelectionMode.FullRowSelect, grid.SelectionMode);
            Assert.False(grid.MultiSelect);
        });
    }

    [Fact]
    public void GridAllowsNativeColumnSorting()
    {
        RunSta(() =>
        {
            using var grid = new LabzDataGrid();
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Ticket", SortMode = DataGridViewColumnSortMode.Automatic
            });
            Assert.Equal(DataGridViewColumnSortMode.Automatic, grid.Columns[0].SortMode);
            grid.StripedRows = false;
            Assert.False(grid.StripedRows);
        });
    }

    private sealed record GridItem(int Id, string Status);

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
