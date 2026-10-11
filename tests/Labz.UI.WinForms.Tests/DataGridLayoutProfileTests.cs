using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridLayoutProfileTests
{
    [Fact]
    public void LayoutRoundTripsVisibilityWidthAndOrder() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        grid.Columns.Add("Status", "Status");
        grid.Columns.Add("Amount", "Amount");
        Assert.True(grid.ColumnReorderingEnabled);
        grid.Columns["Amount"]!.DisplayIndex = 0;
        grid.Columns["Status"]!.Visible = false;
        grid.Columns["Ticket"]!.Width = 145;
        var layout = grid.ExportColumnLayout();
        grid.Columns["Amount"]!.DisplayIndex = 2;
        grid.Columns["Status"]!.Visible = true;
        grid.Columns["Ticket"]!.Width = 80;
        grid.ImportColumnLayout(layout);
        Assert.Equal(0, grid.Columns["Amount"]!.DisplayIndex);
        Assert.False(grid.Columns["Status"]!.Visible);
        Assert.Equal(145, grid.Columns["Ticket"]!.Width);
    });

    [Fact]
    public void LayoutIgnoresUnknownColumns() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        grid.ImportColumnLayout("[{\"Name\":\"Removed\",\"DisplayIndex\":0,\"Visible\":false,\"Width\":100}]");
        Assert.True(grid.Columns["Ticket"]!.Visible);
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
