using Labz.UI.WinForms.Navigation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class AsyncTenantSwitchTests
{
    [Fact]
    public void SuccessfulSwitchCommitsOnlyAfterHostApproval() => RunSta(async () =>
    {
        using var footer = CreateFooter();
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        footer.TenantSwitchHandler = (_, _, _) => gate.Task;
        var pending = footer.TrySelectTenantAsync("b");
        Assert.True(footer.IsTenantSwitching);
        Assert.Equal("a", footer.TenantContext?.Id);
        Assert.False(footer.TrySelectTenant("b"));
        Assert.False(await footer.TrySelectTenantAsync("b"));
        gate.SetResult(true);
        Assert.True(await pending);
        Assert.Equal("b", footer.TenantContext?.Id);
        Assert.False(footer.IsTenantSwitching);
    });

    [Fact]
    public void RejectionAndFailurePreservePreviousTenant() => RunSta(async () =>
    {
        using var footer = CreateFooter();
        footer.TenantSwitchHandler = (_, _, _) => Task.FromResult(false);
        Assert.False(await footer.TrySelectTenantAsync("b"));
        Assert.Equal("a", footer.TenantContext?.Id);
        Exception? failure = null;
        footer.TenantSwitchFailed += (_, e) => failure = e.Exception;
        footer.TenantSwitchHandler = (_, _, _) => Task.FromException<bool>(new InvalidOperationException("failed"));
        Assert.False(await footer.TrySelectTenantAsync("b"));
        Assert.IsType<InvalidOperationException>(failure);
        Assert.Equal("a", footer.TenantContext?.Id);
    });

    private static LabzStatusBar CreateFooter()
    {
        var footer = new LabzStatusBar();
        var a = new LabzTenantContext { Id = "a", DisplayName = "A" };
        var b = new LabzTenantContext { Id = "b", DisplayName = "B" };
        footer.SetAvailableTenants([a, b]);
        footer.TenantContext = a;
        return footer;
    }

    private static void RunSta(Func<Task> action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                // Windows Forms sync context lets continuations return to the STA.
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                Application.Idle += (_, _) => { };
                var task = action();
                while (!task.IsCompleted)
                {
                    Application.DoEvents();
                    Thread.Sleep(1);
                }
                task.GetAwaiter().GetResult();
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
