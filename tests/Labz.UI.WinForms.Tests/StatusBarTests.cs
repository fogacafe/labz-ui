using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class StatusBarTests
{
    [Fact]
    public void ContextCanBeUpdatedWithoutAuthenticationDependencies()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar
            {
                UserName = "Ana", TenantName = "Operations",
                EnvironmentName = "UAT", StatusText = "Connected"
            };
            Assert.Equal("Ana", footer.UserName);
            Assert.Equal("Operations", footer.TenantName);
            Assert.Equal("UAT", footer.EnvironmentName);
            Assert.Equal("Connected", footer.StatusText);
            footer.TenantName = "Research";
            Assert.Equal("Research", footer.TenantName);
        });
    }

    [Fact]
    public void ContextButtonsExposeEvents()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar { UserName = "Ana", TenantName = "Operations" };
            var userClicks = 0;
            var tenantClicks = 0;
            footer.UserRequested += (_, _) => userClicks++;
            footer.TenantRequested += (_, _) => tenantClicks++;
            var buttons = footer.Controls.OfType<FlowLayoutPanel>()
                .SelectMany(panel => panel.Controls.OfType<Button>()).ToArray();
            Assert.Equal(2, buttons.Length);
            foreach (var button in buttons) button.PerformClick();
            Assert.Equal(1, userClicks);
            Assert.Equal(1, tenantClicks);
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
