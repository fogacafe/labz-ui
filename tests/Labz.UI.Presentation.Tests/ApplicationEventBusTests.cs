using Labz.UI.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Labz.UI.Presentation.Tests;

public sealed class ApplicationEventBusTests
{
    private sealed record TradeSaved(int TradeId);

    [Fact]
    public void Two_scopes_exchange_application_events_without_sharing_local_events()
    {
        var services = new ServiceCollection();
        services.AddLabzApplicationEventBus();
        services.AddScoped<IScreenEventBus, ScreenEventBus>();
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var appFirst = first.ServiceProvider.GetRequiredService<IApplicationEventBus>();
        var appSecond = second.ServiceProvider.GetRequiredService<IApplicationEventBus>();
        Assert.Same(appFirst, appSecond);
        var received = 0;
        using var subscription = appSecond.Subscribe<TradeSaved>(e => received = e.TradeId);
        appFirst.Publish(new TradeSaved(1024));
        Assert.Equal(1024, received);

        var localFirst = first.ServiceProvider.GetRequiredService<IScreenEventBus>();
        var localSecond = second.ServiceProvider.GetRequiredService<IScreenEventBus>();
        Assert.NotSame(localFirst, localSecond);
        var localReceived = 0;
        using var localSubscription = localSecond.Subscribe<TradeSaved>(_ => localReceived++);
        localFirst.Publish(new TradeSaved(2048));
        Assert.Equal(0, localReceived);
    }

    [Fact]
    public void Unsubscribing_prevents_later_cross_screen_notifications()
    {
        using var bus = new ApplicationEventBus();
        var count = 0;
        var subscription = bus.Subscribe<TradeSaved>(_ => count++);
        bus.Publish(new TradeSaved(1));
        subscription.Dispose();
        bus.Publish(new TradeSaved(2));
        Assert.Equal(1, count);
    }

    [Fact]
    public void Disposing_bus_rejects_further_use()
    {
        var bus = new ApplicationEventBus();
        bus.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bus.Publish(new TradeSaved(1)));
        Assert.Throws<ObjectDisposedException>(() => bus.Subscribe<TradeSaved>(_ => { }));
    }
}
