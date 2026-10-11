using System.ComponentModel;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridKeyedValidationTests
{
    [Fact]
    public void KeyedMarkersSurviveRowReorderingAndCanBeCleared() => RunSta(() =>
    {
        using var form = new Form();
        using var grid = new LabzDataGrid { Dock = DockStyle.Fill };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ticket", DataPropertyName = "Ticket" });
        grid.AutoGenerateColumns = false;
        form.Controls.Add(grid);
        form.Show();
        var items = new BindingList<Trade> { new("A"), new("B") };
        grid.DataSource = items;
        Application.DoEvents();
        grid.ValidationRowKeySelector = row => (row.DataBoundItem as Trade)?.Ticket;
        grid.SetKeyedRowValidation("A", LabzDataGrid.CellValidationSeverity.Error, "Invalid trade");
        grid.SetKeyedCellValidation("A", "Ticket", LabzDataGrid.CellValidationSeverity.Warning, "Review");
        Assert.True(grid.ClearKeyedCellValidation("A", "Ticket"));
        Assert.False(grid.ClearKeyedCellValidation("A", "Ticket"));
        items.RemoveAt(0);
        items.Add(new Trade("A"));
        Application.DoEvents();
        Assert.Equal("A", ((Trade)grid.Rows[1].DataBoundItem!).Ticket);
        Assert.True(grid.ClearKeyedRowValidation("A"));
        grid.SetKeyedRowValidation("B", LabzDataGrid.CellValidationSeverity.Warning, "Review");
        grid.ClearKeyedValidations();
        Assert.False(grid.ClearKeyedRowValidation("B"));
    });

    [Fact]
    public void KeyedValidationRequiresSelector() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        Assert.Throws<InvalidOperationException>(() =>
            grid.SetKeyedRowValidation("A", LabzDataGrid.CellValidationSeverity.Error, "Invalid"));
    });

    private sealed record Trade(string Ticket);

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
