using Labz.UI.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Labz.UI.Presentation.Tests;

public sealed class ScreenLifecycleIntegrationTests
{
    [Fact]
    public async Task Different_tabs_have_independent_scopes_and_closing_releases_resources()
    {
        await RunStaAsync(async () =>
        {
            var disposed = 0;
            using var services = BuildServices(() => disposed++);
            using var tabs = new TabControl();
            var host = new ScreenWorkspace(services.GetRequiredService<ScreenNavigator>(), tabs);

            var first = host.Open<TestScreen, TestPresenter>("First");
            var second = host.Open<TestScreen, TestPresenter>("Second");
            Assert.NotSame(first.View, second.View);
            Assert.NotSame(first.View.State, second.View.State);
            Assert.Equal(2, tabs.TabCount);

            Assert.True(await host.CloseAsync(first));
            Assert.True(first.View.IsDisposed);
            Assert.Equal(1, disposed);
            Assert.False(second.View.IsDisposed);

            await host.DisposeAsync();
            Assert.True(second.View.IsDisposed);
            Assert.Equal(2, disposed);
        });
    }

    [Fact]
    public async Task Reuse_by_key_returns_same_tab_until_closed()
    {
        await RunStaAsync(async () =>
        {
            using var services = BuildServices(() => { });
            using var tabs = new TabControl();
            var host = new ScreenWorkspace(services.GetRequiredService<ScreenNavigator>(), tabs);

            var first = host.Open<TestScreen, TestPresenter>("First", ScreenOpenPolicy.ReuseByKey, "trade:1");
            var reused = host.Open<TestScreen, TestPresenter>("Again", ScreenOpenPolicy.ReuseByKey, "trade:1");
            Assert.Same(first, reused);
            Assert.Equal(1, tabs.TabCount);

            Assert.True(await host.CloseAsync(first));
            var reopened = host.Open<TestScreen, TestPresenter>("Reopened", ScreenOpenPolicy.ReuseByKey, "trade:1");
            Assert.NotSame(first, reopened);
            Assert.Equal(1, tabs.TabCount);
            await host.DisposeAsync();
        });
    }

    [Fact]
    public async Task Close_during_initialization_cancels_only_that_screen()
    {
        await RunStaAsync(async () =>
        {
            using var services = BuildServices(() => { });
            using var tabs = new TabControl();
            var host = new ScreenWorkspace(services.GetRequiredService<ScreenNavigator>(), tabs);

            var first = host.Open<TestScreen, TestPresenter>("First");
            var second = host.Open<TestScreen, TestPresenter>("Second");
            Assert.True(await host.CloseAsync(first));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.Initialization);
            Assert.False(second.View.IsDisposed);
            await host.DisposeAsync();
        });
    }

    private static ServiceProvider BuildServices(Action onDisposed)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new TrackedState(onDisposed));
        services.AddTransient<TestScreen>();
        services.AddTransient<TestPresenter>();
        services.AddSingleton<DeferredScreenFactory>();
        services.AddTransient<ScreenNavigator>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static Task RunStaAsync(Func<Task> action)
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            using var form = new Form { ShowInTaskbar = false, WindowState = FormWindowState.Minimized };
            form.Shown += async (_, _) =>
            {
                try
                {
                    await action();
                    result.TrySetResult();
                }
                catch (Exception ex) { result.TrySetException(ex); }
                finally { form.Close(); }
            };
            try { Application.Run(form); }
            catch (Exception ex) { result.TrySetException(ex); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return result.Task.WaitAsync(TimeSpan.FromSeconds(30));
    }

    private sealed class TrackedState(Action onDisposed) : IDisposable
    {
        public Guid Id { get; } = Guid.NewGuid();
        public void Dispose() => onDisposed();
    }

    private sealed class TestScreen(TrackedState state) : Form
    {
        public TrackedState State { get; } = state;
    }

    private sealed class TestPresenter : IAsyncScreenPresenter<TestScreen>
    {
        public void Attach(TestScreen view) { }
        public Task InitializeAsync(CancellationToken cancellationToken) =>
            Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        public Task CloseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
