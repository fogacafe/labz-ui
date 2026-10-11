using System.ComponentModel;
using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

/// <summary>Windows STA smoke checks for theme changes, binding, and core input interactions.</summary>
public class ShowcaseSmokeTests
{
    [Fact]
    public void AllNewControlsSurviveThemeChanges()
    {
        RunSta(() =>
        {
            var original = UiThemeManager.Current;
            try
            {
                using var form = new Form();
                form.Controls.AddRange([
                    new LabzTabs { TabPages = { new TabPage("One"), new TabPage("Two") } },
                    new LabzDatePicker(),
                    new LabzNumericInput { NumericFormat = LabzNumericFormat.Currency },
                    new LabzBadge { Text = "Ready", Variant = LabzBadgeVariant.Success },
                    new LabzCard { Title = "Card" },
                    new LabzSpinner { Active = false },
                    new LabzEmptyState { Heading = "No data" },
                    new LabzDataGrid()
                ]);
                form.CreateControl();
                UiThemeManager.Apply(UiTheme.Dark);
                UiThemeManager.Apply(UiTheme.Light);
                foreach (Control control in form.Controls)
                    Assert.False(control.IsDisposed);
            }
            finally { UiThemeManager.Apply(original); }
        });
    }

    [Fact]
    public void GridSupportsBoundRowsAndSelection()
    {
        RunSta(() =>
        {
            using var form = new Form();
            using var grid = new LabzDataGrid { Dock = DockStyle.Fill };
            form.Controls.Add(grid);
            var data = new BindingList<GridRow> { new(1, "Draft"), new(2, "Saved") };
            grid.DataSource = data;
            form.Show();
            Application.DoEvents();
            Assert.Equal(2, grid.Rows.Count);
            grid.Rows[1].Selected = true;
            Assert.True(grid.Rows[1].Selected);
            data.Add(new GridRow(3, "Released"));
            Application.DoEvents();
            Assert.Equal(3, grid.Rows.Count);
            form.Hide();
        });
    }

    [Fact]
    public void NativeKeyboardAndInputContractsRemainAvailable()
    {
        RunSta(() =>
        {
            using var tabs = new LabzTabs();
            tabs.TabPages.AddRange([new TabPage("First"), new TabPage("Second")]);
            tabs.SelectedIndex = 1;
            Assert.Equal("Second", tabs.SelectedTab?.Text);

            using var date = new LabzDatePicker { ShowTime = true };
            Assert.True(date.ShowTime);

            using var number = new LabzNumericInput
            {
                CultureName = "pt-BR",
                NumericFormat = LabzNumericFormat.Percent,
                Value = 25m
            };
            Assert.Equal(25m, number.Value);
            Assert.Contains("%", number.Text);
        });
    }

    private sealed record GridRow(int Id, string Status);

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
