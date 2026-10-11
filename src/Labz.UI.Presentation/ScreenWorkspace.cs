using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>
/// Hosts heterogeneous deferred screens in one WinForms TabControl.
/// Use on the UI thread; the workspace owns attached screens and their disposal.
/// </summary>
public sealed class ScreenWorkspace : IAsyncDisposable
{
    private readonly TabControl _tabs;
    private readonly ScreenNavigator _navigator;
    private readonly Dictionary<object, (TabPage Page, Func<ValueTask> Close)> _screens = new();
    private readonly HashSet<Task> _closing = new();
    private readonly HashSet<object> _closePending = new();
    private readonly HashSet<object> _closePending = new();
    private bool _disposed;

    /// <summary>Optional per-screen close approval. Return false to keep the tab open.</summary>
    public Func<object, CancellationToken, ValueTask<bool>>? CanCloseAsync { get; set; }

    /// <summary>Optional per-screen close approval. Return false to keep the tab open.</summary>
    public Func<object, CancellationToken, ValueTask<bool>>? CanCloseAsync { get; set; }

    public ScreenWorkspace(ScreenNavigator navigator, TabControl tabs)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        _tabs = tabs ?? throw new ArgumentNullException(nameof(tabs));
    }

    public DeferredScreenInstance<TView> Open<TView, TPresenter>(
        string title,
        ScreenOpenPolicy policy = ScreenOpenPolicy.NewInstance,
        string? key = null,
        CancellationToken cancellationToken = default)
        where TView : Form
        where TPresenter : class, IAsyncScreenPresenter<TView>
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var screen = _navigator.Open<TView, TPresenter>(policy, key, cancellationToken);
        if (_screens.TryGetValue(screen, out var existing))
        {
            _tabs.SelectedTab = existing.Page;
            return screen;
        }

        var page = new TabPage(title);
        try
        {
            screen.View.TopLevel = false;
            screen.View.FormBorderStyle = FormBorderStyle.None;
            screen.View.Dock = DockStyle.Fill;
            page.Controls.Add(screen.View);
            _tabs.TabPages.Add(page);
            _screens.Add(screen, (page, async () =>
            {
                _navigator.Forget(screen);
                await screen.DisposeAsync();
            }));
            screen.View.Show();
            _tabs.SelectedTab = page;
            return screen;
        }
        catch
        {
            _screens.Remove(screen);
            _tabs.TabPages.Remove(page);
            page.Dispose();
            _navigator.Forget(screen);
            _ = screen.DisposeAsync();
            throw;
        }
    }

    public bool TryGetPage<TView>(DeferredScreenInstance<TView> screen, out TabPage? page)
        where TView : Form
    {
        if (_screens.TryGetValue(screen, out var entry))
        {
            page = entry.Page;
            return true;
        }
        page = null;
        return false;
    }

    public Task<bool> CloseSelectedAsync(CancellationToken cancellationToken = default)
    {
        var entry = _screens.FirstOrDefault(x => ReferenceEquals(x.Value.Page, _tabs.SelectedTab));
        return entry.Key is null ? Task.FromResult(false) : CloseAsync(entry.Key, cancellationToken);
    }

    public async Task<bool> CloseAsync(object screen, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(screen);
        if (!_screens.ContainsKey(screen) || !_closePending.Add(screen)) return false;
        try
        {
            if (CanCloseAsync is { } guard && !await guard(screen, cancellationToken)) return false;
            cancellationToken.ThrowIfCancellationRequested();
            if (!_screens.Remove(screen, out var entry)) return false;
            _tabs.TabPages.Remove(entry.Page);
            var closing = CloseCoreAsync(entry);
            _closing.Add(closing);
            try { await closing; }
            finally { _closing.Remove(closing); }
            return true;
        }
        finally { _closePending.Remove(screen); }
    }

    private static async Task CloseCoreAsync((TabPage Page, Func<ValueTask> Close) entry)
    {
        try { await entry.Close(); }
        finally { entry.Page.Dispose(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        // Disposal is forced: callers should request guarded close before shutdown.
        foreach (var screen in _screens.Keys.ToArray())
        {
            if (!_screens.Remove(screen, out var entry)) continue;
            _tabs.TabPages.Remove(entry.Page);
            var closing = CloseCoreAsync(entry);
            _closing.Add(closing);
            try { await closing; }
            finally { _closing.Remove(closing); }
        }
        await Task.WhenAll(_closing.ToArray());
    }
}
