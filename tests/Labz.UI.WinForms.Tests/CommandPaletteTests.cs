using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class CommandPaletteTests
{
    private static readonly PaletteEntry[] Entries =
    [
        new("bonds", "Public Bonds", "trades", "Ticket Entry", "Operations"),
        new("bonds", "Public Bonds", "import", "Import", "Operations"),
        new("funds", "Funds", "positions", "Positions", "Queries")
    ];

    [Fact]
    public void SearchMatchesPageModuleAndGroupCaseInsensitively()
    {
        Assert.Single(LabzCommandPalette.Search(Entries, "ticket"));
        Assert.Equal(2, LabzCommandPalette.Search(Entries, "PUBLIC bonds").Count);
        Assert.Equal("positions", Assert.Single(LabzCommandPalette.Search(Entries, "funds queries")).PageId);
        Assert.Empty(LabzCommandPalette.Search(Entries, "missing"));
        Assert.Equal(3, LabzCommandPalette.Search(Entries, "").Count);
    }

    [Fact]
    public void DialogBuildsFromRegisteredModules()
    {
        RunSta(() =>
        {
            var module = new LabzModule("bonds", "Public Bonds");
            module.Pages.Add(new LabzNavigationPage("trades", "Ticket Entry"));
            using var dialog = new LabzCommandPalette([module], (_, _) => { });
            Assert.Equal("Go to screen", dialog.Text);
        });
    }

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
