using System.ComponentModel;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class DataGridGroupingTests
{
    [Fact]
    public void GroupsCanCollapseAndExpand() => RunSta(() =>
    {
        using var host = new Form();
        using var grid = new LabzDataGrid { Dock = DockStyle.Fill };
        grid.AutoGenerateColumns = true;
        host.Controls.Add(grid);
        host.Show();
        grid.DataSource = new BindingList<Trade>
        {
            new("A", "XP"), new("B", "XP"), new("C", "BTG")
        };
        Application.DoEvents();
        grid.GroupRows(row => ((Trade)row.DataBoundItem!).Counterparty);
        Assert.Equal(2, grid.GetGroups().Count);
        Assert.Contains(grid.GetGroups(), group => group.Key == "XP" && group.Count == 2);
        Assert.True(grid.SetGroupExpanded("XP", false));
        Assert.False(grid.Rows[0].Visible);
        Assert.False(grid.Rows[1].Visible);
        Assert.True(grid.Rows[2].Visible);
        Assert.True(grid.SetGroupExpanded("XP", true));
        Assert.True(grid.Rows[0].Visible);
        grid.ClearGrouping();
        Assert.Empty(grid.GetGroups());
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
