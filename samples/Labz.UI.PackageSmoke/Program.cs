using Labz.UI.WinForms.Controls;

namespace Labz.UI.PackageSmoke;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form { Text = "Labz package smoke", Width = 640, Height = 420 };
        using var grid = new LabzDataGrid { Dock = DockStyle.Fill };
        grid.Columns.Add("Id", "Ticket");
        grid.Rows.Add("T-1001");
        form.Controls.Add(grid);
        Application.Run(form);
    }
}
