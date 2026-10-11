using Labz.UI.Presentation;
using Xunit;

namespace Labz.UI.Presentation.Tests;

public sealed class ScreenEventBusTests
{
    private sealed record TradeSelected(int Id);
    private sealed record TradeSaved(int Id);

    [Fact]
    public void Publish_delivers_only_matching_typed_events()
    {
        using var bus = new ScreenEventBus();
        var selected = new List<int>();
        var saved = 0;
        using var a = bus.Subscribe<TradeSelected>(e => selected.Add(e.Id));
        using var b = bus.Subscribe<TradeSaved>(_ => saved++);
        bus.Publish(new TradeSelected(42));
        Assert.Equal(new[] { 42 }, selected);
        Assert.Equal(0, saved);
    }

    [Fact]
    public void Subscription_can_be_disposed_multiple_times()
    {
        using var bus = new ScreenEventBus();
        var calls = 0;
        var subscription = bus.Subscribe<TradeSelected>(_ => calls++);
        subscription.Dispose();
        subscription.Dispose();
        bus.Publish(new TradeSelected(1));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Two_screen_buses_are_isolated()
    {
        using var first = new ScreenEventBus();
        using var second = new ScreenEventBus();
        var firstCalls = 0;
        var secondCalls = 0;
        using var a = first.Subscribe<TradeSelected>(_ => firstCalls++);
        using var b = second.Subscribe<TradeSelected>(_ => secondCalls++);
        first.Publish(new TradeSelected(1));
        Assert.Equal(1, firstCalls);
        Assert.Equal(0, secondCalls);
    }

    [Fact]
    public void Disposing_bus_rejects_new_subscriptions_and_publications()
    {
        var bus = new ScreenEventBus();
        var subscription = bus.Subscribe<TradeSelected>(_ => { });
        bus.Dispose();
        subscription.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bus.Subscribe<TradeSelected>(_ => { }));
        Assert.Throws<ObjectDisposedException>(() => bus.Publish(new TradeSelected(1)));
    }

    [Fact]
    public void Snapshot_delivery_allows_unsubscribe_during_publication()
    {
        using var bus = new ScreenEventBus();
        IDisposable? second = null;
        var calls = 0;
        using var first = bus.Subscribe<TradeSelected>(_ => { calls++; second?.Dispose(); });
        second = bus.Subscribe<TradeSelected>(_ => calls++);
        bus.Publish(new TradeSelected(1));
        bus.Publish(new TradeSelected(2));
        Assert.Equal(3, calls);
    }
}
