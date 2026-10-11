using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class StatusBarVisualTests
{
    [Fact]
    public void EnvironmentAccentsDistinguishProductionFromDevelopment()
    {
        Assert.NotEqual(LabzStatusBar.GetEnvironmentColor("PROD"), LabzStatusBar.GetEnvironmentColor("DEV"));
        Assert.Equal(LabzStatusBar.GetEnvironmentColor("production"), LabzStatusBar.GetEnvironmentColor("PROD"));
        Assert.Equal(LabzStatusBar.GetEnvironmentColor("hml"), LabzStatusBar.GetEnvironmentColor("UAT"));
        Assert.Equal(LabzStatusBar.GetEnvironmentColor(null), LabzStatusBar.GetEnvironmentColor("other"));
    }

    [Fact]
    public void FooterRemainsCompactAndContextIsOptional()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar { EnvironmentName = "DEV" };
            Assert.Equal(32, footer.Height);
            Assert.Null(footer.UserContext);
            Assert.Null(footer.TenantContext);
            footer.UserContext = new LabzUserContext { Id = "u", DisplayName = "Ana" };
            Assert.Equal("Ana", footer.UserName);
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
