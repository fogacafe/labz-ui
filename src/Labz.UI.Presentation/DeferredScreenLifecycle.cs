using Microsoft.Extensions.DependencyInjection;
using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>
/// An immediately available Form and an observable asynchronous initialization.
/// Host must invoke and dispose this object on the WinForms UI thread.
/// </summary>
public sealed class DeferredScreenInstance<TView> : IAsyncDisposable where TView : Form
{
    private readonly AsyncServiceScope _scope;
    private readonly IAsyncScreenPresenter<TView> _presenter;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _closed;

    internal DeferredScreenInstance(AsyncServiceScope scope, TView view, IAsyncScreenPresenter<TView> presenter)
    {
        _scope = scope;
        View = view;
        _presenter = presenter;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public TView View { get; }
    public CancellationToken LifetimeToken => _lifetime.Token;
    public Task Initialization => _completion.Task;

    internal void Start(CancellationToken cancellationToken)
    {
        _ = RunInitializationAsync(cancellationToken);
    }

    private async Task RunInitializationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
            // Yield once so the caller can attach the Form before initialization begins.
            await Task.Yield();
            linked.Token.ThrowIfCancellationRequested();
            await _presenter.InitializeAsync(linked.Token);
            _completion.TrySetResult();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            _completion.TrySetCanceled();
        }
        catch (Exception error)
        {
            _completion.TrySetException(error);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0) return;
        _lifetime.Cancel();
        try
        {
            // Initialization must observe cancellation to avoid indefinite close.
            try { await Initialization; }
            catch (OperationCanceledException) { }
            catch { /* Initialization is observable separately; always clean up. */ }
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

/// <summary>
/// Creates a Form synchronously, then starts its asynchronous initialization.
/// Initialization failures are exposed by DeferredScreenInstance.Initialization.
/// </summary>
public sealed class DeferredScreenFactory(IServiceScopeFactory scopes)
{
    /// <summary>Creates a screen and delivers typed parameters before initialization starts.</summary>
    public DeferredScreenInstance<TView> Open<TView, TPresenter, TParameters>(
        TParameters parameters,
        CancellationToken cancellationToken = default)
        where TView : Form
        where TPresenter : class, IAsyncScreenPresenter<TView>, IScreenParameters<TParameters>
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = scopes.CreateAsyncScope();
        try
        {
            var view = scope.ServiceProvider.GetRequiredService<TView>();
            var presenter = scope.ServiceProvider.GetRequiredService<TPresenter>();
            presenter.Attach(view);
            presenter.SetParameters(parameters);
            var instance = new DeferredScreenInstance<TView>(scope, view, presenter);
            instance.Start(cancellationToken);
            return instance;
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    public DeferredScreenInstance<TView> Open<TView, TPresenter>(
        CancellationToken cancellationToken = default)
        where TView : Form
        where TPresenter : class, IAsyncScreenPresenter<TView>
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = scopes.CreateAsyncScope();
        try
        {
            var view = scope.ServiceProvider.GetRequiredService<TView>();
            var presenter = scope.ServiceProvider.GetRequiredService<TPresenter>();
            presenter.Attach(view);
            var instance = new DeferredScreenInstance<TView>(scope, view, presenter);
            instance.Start(cancellationToken);
            return instance;
        }
        catch
        {
            // Synchronous activation failure: best effort disposal of the async scope.
            scope.Dispose();
            throw;
        }
    }
}
