using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class MultiTenantStatusBarTests
{
    [Fact]
    public void SwitchesToAllowedTenantAndNotifiesHost()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar();
            var a = new LabzTenantContext { Id = "a", DisplayName = "Desk A" };
            var b = new LabzTenantContext { Id = "b", DisplayName = "Desk B" };
            footer.SetAvailableTenants([a, b]);
            footer.TenantContext = a;
            string? selected = null;
            footer.TenantChanged += (_, e) => selected = e.Tenant.Id;

            Assert.True(footer.TrySelectTenant("B"));
            Assert.Equal("b", footer.TenantContext?.Id);
            Assert.Equal("Desk B", footer.TenantName);
            Assert.Equal("b", selected);
            Assert.False(footer.TrySelectTenant("unknown"));
            Assert.False(footer.TrySelectTenant("b"));
        });
    }

    [Fact]
    public void CancelledSwitchRetainsCurrentTenant()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar();
            var a = new LabzTenantContext { Id = "a", DisplayName = "Desk A" };
            var b = new LabzTenantContext { Id = "b", DisplayName = "Desk B" };
            footer.SetAvailableTenants([a, b]);
            footer.TenantContext = a;
            var changed = 0;
            footer.TenantChanging += (_, e) =>
            {
                Assert.Equal("a", e.Current?.Id);
                Assert.Equal("b", e.Requested.Id);
                e.Cancel = true;
            };
            footer.TenantChanged += (_, _) => changed++;
            Assert.False(footer.TrySelectTenant("b"));
            Assert.Equal("a", footer.TenantContext?.Id);
            Assert.Equal(0, changed);
        });
    }

    [Fact]
    public void RejectsDuplicateTenantIds()
    {
        RunSta(() =>
        {
            using var footer = new LabzStatusBar();
            Assert.Throws<ArgumentException>(() => footer.SetAvailableTenants([
                new LabzTenantContext { Id = "A", DisplayName = "First" },
                new LabzTenantContext { Id = "a", DisplayName = "Second" }
            ]));
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
