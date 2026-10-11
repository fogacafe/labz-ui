using System.ComponentModel;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridIntegrationTests
{
    [Fact]
    public void BoundTradeWorkflowCombinesSearchValidationAndGrouping() => RunSta(() =>
    {
        using var form = new Form();
        using var grid = new LabzDataGrid { Dock = DockStyle.Fill, AutoGenerateColumns = false };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Ticket", DataPropertyName = "Ticket" });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Counterparty", DataPropertyName = "Counterparty" });
        form.Controls.Add(grid);
        form.Show();
        var trades = new BindingList<Trade>
        {
            new("T1", "XP"), new("T2", "XP"), new("T3", "BTG")
        };
        grid.DataSource = trades;
        Application.DoEvents();

        grid.EditableGrid = true;
        grid.ValidationRowKeySelector = row => (row.DataBoundItem as Trade)?.Ticket;
        grid.SetKeyedRowValidation("T2", LabzDataGrid.CellValidationSeverity.Warning, "Revalidate");
        grid.SetKeyedCellValidation("T2", "Counterparty", LabzDataGrid.CellValidationSeverity.Error, "Review");
        Assert.True(grid.TryFindNext("T2"));
        Assert.Equal("T2", ((Trade)grid.CurrentRow!.DataBoundItem!).Ticket);

        grid.GroupRows(row => ((Trade)row.DataBoundItem!).Counterparty);
        Assert.Contains(grid.GetGroups(), g => g.Key == "XP" && g.Count == 2);
        Assert.True(grid.SetGroupExpanded("XP", false));
        Assert.False(grid.Rows[1].Visible);
        Assert.True(grid.SetGroupExpanded("XP", true));
        Assert.True(grid.Rows[1].Visible);

        trades[1] = new Trade("T2", "BTG");
        Application.DoEvents();
        grid.RefreshGrouping();
        Assert.Contains(grid.GetGroups(), g => g.Key == "BTG" && g.Count == 2);
        Assert.True(grid.ClearKeyedRowValidation("T2"));
        Assert.True(grid.ClearKeyedCellValidation("T2", "Counterparty"));
    });

    [Fact]
    public void ColumnLayoutRoundTripPreservesVisibleColumns() => RunSta(() =>
    {
        using var grid = new LabzDataGrid();
        grid.Columns.Add("Ticket", "Ticket");
        grid.Columns.Add("Quantity", "Quantity");
        grid.Columns["Quantity"]!.Visible = false;
        var json = grid.ExportColumnLayout();
        grid.Columns["Quantity"]!.Visible = true;
        grid.ImportColumnLayout(json);
        Assert.False(grid.Columns["Quantity"]!.Visible);
    });

    private sealed record Trade(string Ticket, string Counterparty);

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
