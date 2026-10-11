using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>
/// A simple TabControl host for deferred screens. Must be accessed on the WinForms UI thread.
/// The host owns tab attachment and screen disposal; navigator owns keyed lookup.
/// </summary>
public sealed class ScreenTabHost<TView, TPresenter> : IAsyncDisposable
    where TView : Form
    where TPresenter : class, IAsyncScreenPresenter<TView>
{
    private readonly ScreenNavigator _navigator;
    private readonly TabControl _tabs;
    private readonly Dictionary<DeferredScreenInstance<TView>, TabPage> _pages = new();
    private readonly HashSet<Task> _closing = new();
    private bool _disposed;

    public ScreenTabHost(ScreenNavigator navigator, TabControl tabs)
    {
        _navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        _tabs = tabs ?? throw new ArgumentNullException(nameof(tabs));
    }

    public DeferredScreenInstance<TView> Open(
        string title,
        ScreenOpenPolicy policy = ScreenOpenPolicy.NewInstance,
        string? key = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var screen = _navigator.Open<TView, TPresenter>(policy, key, cancellationToken);
        if (_pages.TryGetValue(screen, out var existing))
        {
            _tabs.SelectedTab = existing;
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
            _pages.Add(screen, page);
            screen.View.Show();
            _tabs.SelectedTab = page;
            return screen;
        }
        catch
        {
            _tabs.TabPages.Remove(page);
            page.Dispose();
            _navigator.Forget(screen);
            _ = screen.DisposeAsync();
            throw;
        }
    }

    public async Task<bool> CloseSelectedAsync()
    {
        var entry = _pages.FirstOrDefault(x => ReferenceEquals(x.Value, _tabs.SelectedTab));
        return entry.Key is not null && await CloseAsync(entry.Key);
    }

    public async Task<bool> CloseAsync(DeferredScreenInstance<TView> screen)
    {
        if (!_pages.Remove(screen, out var page)) return false;
        _navigator.Forget(screen);
        _tabs.TabPages.Remove(page);
        var closing = CloseCoreAsync(screen, page);
        _closing.Add(closing);
        try { await closing; }
        finally { _closing.Remove(closing); }
        return true;
    }

    private static async Task CloseCoreAsync(DeferredScreenInstance<TView> screen, TabPage page)
    {
        try { await screen.DisposeAsync(); }
        finally { page.Dispose(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var screen in _pages.Keys.ToArray())
            await CloseAsync(screen);
        await Task.WhenAll(_closing.ToArray());
    }
}
