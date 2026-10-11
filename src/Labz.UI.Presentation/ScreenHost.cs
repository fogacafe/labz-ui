using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>A presenter initialized once after its view has been constructed.</summary>
public interface IScreenPresenter<in TView> where TView : Form
{
    void Attach(TView view);
}

/// <summary>Owns the Form, presenter and DI scope of one independently opened tab.</summary>
public sealed class ScreenInstance<TView> : IDisposable where TView : Form
{
    private readonly IServiceScope _scope;
    private bool _disposed;

    internal ScreenInstance(IServiceScope scope, TView view)
    {
        _scope = scope;
        View = view;
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
    public TView View { get; }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { View.Dispose(); }
        finally { _scope.Dispose(); }
    }
}

/// <summary>Creates a fresh DI scope and a fresh Form for every call.</summary>
public sealed class ScreenFactory(IServiceScopeFactory scopes)
{
    public ScreenInstance<TView> Open<TView, TPresenter>()
        where TView : Form
        where TPresenter : class, IScreenPresenter<TView>
    {
        var scope = scopes.CreateScope();
        try
        {
            var view = scope.ServiceProvider.GetRequiredService<TView>();
            var presenter = scope.ServiceProvider.GetRequiredService<TPresenter>();
            presenter.Attach(view);
            return new ScreenInstance<TView>(scope, view);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}

/// <summary>Explicit registrations keep presenter dependencies visible to DI.</summary>
public static class ScreenServiceCollectionExtensions
{
    public static IServiceCollection AddLabzScreen<TView, TPresenter>(this IServiceCollection services)
        where TView : Form
        where TPresenter : class, IScreenPresenter<TView>
    {
        services.AddTransient<TView>();
        services.AddTransient<TPresenter>();
        return services;
    }
}
