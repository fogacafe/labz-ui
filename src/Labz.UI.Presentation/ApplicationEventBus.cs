using Microsoft.Extensions.DependencyInjection;

namespace Labz.UI.Presentation;

/// <summary>
/// Application-wide typed events shared across screen scopes.
/// Handlers run synchronously on the publishing thread; marshal to the UI thread when needed.
/// </summary>
public interface IApplicationEventBus : IScreenEventBus { }

/// <summary>
/// Singleton event bus for cross-screen notifications. Subscribers own and dispose their tokens.
/// </summary>
public sealed class ApplicationEventBus : IApplicationEventBus, IDisposable
{
    private readonly ScreenEventBus _inner = new();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler) => _inner.Subscribe(handler);
    public void Publish<TEvent>(TEvent message) => _inner.Publish(message);
    public void Dispose() => _inner.Dispose();
}

public static class ApplicationEventBusServiceCollectionExtensions
{
    /// <summary>Registers a shared event bus without replacing scoped IScreenEventBus.</summary>
    public static IServiceCollection AddLabzApplicationEventBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IApplicationEventBus, ApplicationEventBus>();
        return services;
    }
}
