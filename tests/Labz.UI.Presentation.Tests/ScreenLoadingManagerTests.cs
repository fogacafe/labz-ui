using Labz.UI.Presentation;
using Xunit;

namespace Labz.UI.Presentation.Tests;

public sealed class ScreenLoadingManagerTests
{
    [Fact]
    public void Concurrent_operations_keep_loading_until_last_token_ends()
    {
        using var manager = new ScreenLoadingManager();
        var first = manager.Begin("First");
        var second = manager.Begin("Second");
        var third = manager.Begin("Third");
        Assert.True(manager.IsLoading);
        Assert.Equal(3, manager.ActiveCount);
        Assert.Equal("Third", manager.Message);
        second.Dispose();
        Assert.Equal(2, manager.ActiveCount);
        Assert.Equal("Third", manager.Message);
        third.Dispose();
        Assert.Equal("First", manager.Message);
        first.Dispose();
        Assert.False(manager.IsLoading);
        Assert.Null(manager.Message);
    }

    [Fact]
    public void Dispose_token_is_idempotent()
    {
        using var manager = new ScreenLoadingManager();
        var token = manager.Begin();
        token.Dispose();
        token.Dispose();
        Assert.Equal(0, manager.ActiveCount);
    }

    [Fact]
    public void Change_notification_reports_begin_and_end()
    {
        using var manager = new ScreenLoadingManager();
        var count = 0;
        manager.Changed += (_, _) => count++;
        using (manager.Begin("Loading")) Assert.True(manager.IsLoading);
        Assert.Equal(2, count);
    }

    [Fact]
    public void Disposing_manager_invalidates_tokens_and_future_begin()
    {
        var manager = new ScreenLoadingManager();
        var token = manager.Begin();
        manager.Dispose();
        token.Dispose();
        Assert.False(manager.IsLoading);
        Assert.Throws<ObjectDisposedException>(() => manager.Begin());
    }

    [Fact]
    public void Independent_managers_do_not_share_loading_state()
    {
        using var first = new ScreenLoadingManager();
        using var second = new ScreenLoadingManager();
        using var token = first.Begin();
        Assert.True(first.IsLoading);
        Assert.False(second.IsLoading);
    }
}
