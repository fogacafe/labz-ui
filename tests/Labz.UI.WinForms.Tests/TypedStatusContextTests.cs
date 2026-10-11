using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class TypedStatusContextTests
{
    [Fact]
    public void TypedContextsPopulateLegacyLabelsAndExposeIds()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar();
            footer.UserContext = new LabzUserContext { Id = "u-42", DisplayName = "Ana", Email = "ana@example.test" };
            footer.TenantContext = new LabzTenantContext { Id = "t-1", DisplayName = "Trading" };
            Assert.Equal("Ana", footer.UserName);
            Assert.Equal("Trading", footer.TenantName);
            Assert.Equal("u-42", footer.UserContext?.Id);
            Assert.Equal("t-1", footer.TenantContext?.Id);

            footer.UserName = "Legacy";
            Assert.Null(footer.UserContext);
            Assert.Equal("Legacy", footer.UserName);
            footer.TenantContext = null;
            Assert.Null(footer.TenantName);
        });
    }

    [Fact]
    public void ContextEventsCarryCurrentIdentityAndPreserveLegacyEvents()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar
            {
                UserContext = new LabzUserContext { Id = "user-1", DisplayName = "Ana" },
                TenantContext = new LabzTenantContext { Id = "tenant-2", DisplayName = "Desk" }
            };
            string? userId = null;
            string? tenantId = null;
            var legacy = 0;
            footer.UserContextRequested += (_, e) => userId = e.Context?.Id;
            footer.TenantContextRequested += (_, e) => tenantId = e.Context?.Id;
            footer.UserRequested += (_, _) => legacy++;
            footer.TenantRequested += (_, _) => legacy++;
            foreach (var button in footer.Controls.OfType<FlowLayoutPanel>()
                .SelectMany(panel => panel.Controls.OfType<Button>()))
                button.PerformClick();
            Assert.Equal("user-1", userId);
            Assert.Equal("tenant-2", tenantId);
            Assert.Equal(2, legacy);
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
