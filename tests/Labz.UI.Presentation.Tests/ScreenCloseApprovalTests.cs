using Labz.UI.Presentation;
using Xunit;

namespace Labz.UI.Presentation.Tests;

public sealed class ScreenCloseApprovalTests
{
    [Fact]
    public async Task All_approved_returns_true_in_order()
    {
        var visited = new List<int>();
        var result = await ScreenCloseApproval.ApproveAllAsync(
            new[] { 1, 2, 3 },
            (id, _) =>
            {
                visited.Add(id);
                return ValueTask.FromResult(true);
            });
        Assert.True(result);
        Assert.Equal(new[] { 1, 2, 3 }, visited);
    }

    [Fact]
    public async Task Veto_stops_before_later_screens()
    {
        var visited = new List<int>();
        var result = await ScreenCloseApproval.ApproveAllAsync(
            new[] { 1, 2, 3 },
            (id, _) =>
            {
                visited.Add(id);
                return ValueTask.FromResult(id != 2);
            });
        Assert.False(result);
        Assert.Equal(new[] { 1, 2 }, visited);
    }

    [Fact]
    public async Task Missing_guard_approves_all()
    {
        Assert.True(await ScreenCloseApproval.ApproveAllAsync(new[] { 1, 2 }, null));
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        using var cts = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await ScreenCloseApproval.ApproveAllAsync(new[] { 1 }, (id, _) =>
            {
                cts.Cancel();
                return ValueTask.FromResult(true);
            }, cts.Token));
    }

    [Fact]
    public async Task Guard_exception_is_not_swallowed()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ScreenCloseApproval.ApproveAllAsync(new[] { 1 },
                (id, _) => throw new InvalidOperationException("save failed")));
    }

    [Fact]
    public async Task Async_guard_is_awaited()
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = ScreenCloseApproval.ApproveAllAsync(new[] { 1 },
            async (id, _) => await completion.Task);
        Assert.False(pending.IsCompleted);
        completion.SetResult(true);
        Assert.True(await pending);
    }
}
