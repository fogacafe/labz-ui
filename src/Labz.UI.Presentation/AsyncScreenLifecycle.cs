using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>Optional async initialization and shutdown hooks for a screen presenter.</summary>
public interface IAsyncScreenPresenter<in TView> : IScreenPresenter<TView> where TView : Form
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
}

/// <summary>Owns an async presenter, cancellation source, Form and DI scope.</summary>
public sealed class AsyncScreenInstance<TView> : IAsyncDisposable where TView : Form
{
    private readonly AsyncServiceScope _scope;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly IAsyncScreenPresenter<TView> _presenter;
    private int _disposed;

    internal AsyncScreenInstance(AsyncServiceScope scope, TView view, IAsyncScreenPresenter<TView> presenter)
    {
        _scope = scope;
        View = view;
        _presenter = presenter;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public TView View { get; }
    public CancellationToken LifetimeToken => _lifetime.Token;

    internal Task InitializeAsync(CancellationToken cancellationToken)
        => _presenter.InitializeAsync(cancellationToken);

    /// <summary>Cancel this screen only, run presenter cleanup, then dispose resources.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _lifetime.Cancel();
        try
        {
            await _presenter.CloseAsync(CancellationToken.None);
        }
        finally
        {
            try { View.Dispose(); }
            finally
            {
                try { await _scope.DisposeAsync(); }
                finally { _lifetime.Dispose(); }
            }
        }
    }
}

/// <summary>Experimental asynchronous creation; call on the WinForms UI thread.</summary>
public sealed class AsyncScreenFactory(IServiceScopeFactory scopes)
{
    public async Task<AsyncScreenInstance<TView>> OpenAsync<TView, TPresenter>(
        CancellationToken cancellationToken = default)
        where TView : Form
        where TPresenter : class, IAsyncScreenPresenter<TView>
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = scopes.CreateAsyncScope();
        AsyncScreenInstance<TView>? instance = null;
        try
        {
            var view = scope.ServiceProvider.GetRequiredService<TView>();
            var presenter = scope.ServiceProvider.GetRequiredService<TPresenter>();
            presenter.Attach(view);
            instance = new AsyncScreenInstance<TView>(scope, view, presenter);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, instance.LifetimeToken);
            await instance.InitializeAsync(linked.Token);
            return instance;
        }
        catch
        {
            if (instance is not null) await instance.DisposeAsync();
            else await scope.DisposeAsync();
            throw;
        }
    }
}
