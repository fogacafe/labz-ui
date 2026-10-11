using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridCellValidationTests
{
    [Fact]
    public void EditingAndValidationMarkersCanBeManaged() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        grid.Rows.Add("A");
        Assert.False(grid.EditableGrid);
        grid.EditableGrid = true;
        Assert.False(grid.ReadOnly);
        grid.SetCellValidation(0, 0, LabzDataGrid.CellValidationSeverity.Error, "Invalid ticket");
        Assert.True(grid.ClearCellValidation(0, 0));
        Assert.False(grid.ClearCellValidation(0, 0));
        grid.SetCellValidation(0, 0, LabzDataGrid.CellValidationSeverity.Warning, "Check value");
        grid.SetRowValidation(0, LabzDataGrid.CellValidationSeverity.Error, "Revalidate entire trade");
        Assert.True(grid.ClearRowValidation(0));
        Assert.False(grid.ClearRowValidation(0));
        grid.SetRowValidation(0, LabzDataGrid.CellValidationSeverity.Warning, "Review trade");
        grid.ClearRowValidations();
        Assert.False(grid.ClearRowValidation(0));
        grid.ClearCellValidations();
        Assert.False(grid.ClearCellValidation(0, 0));
    });

    [Fact]
    public void InvalidCellIndicesAreRejected() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            grid.SetCellValidation(0, 0, LabzDataGrid.CellValidationSeverity.Error, "Invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            grid.SetRowValidation(0, LabzDataGrid.CellValidationSeverity.Error, "Invalid"));
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
