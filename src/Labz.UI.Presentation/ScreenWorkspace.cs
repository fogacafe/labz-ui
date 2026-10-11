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
    private bool _disposed;

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

    public Task<bool> CloseSelectedAsync()
    {
        var entry = _screens.FirstOrDefault(x => ReferenceEquals(x.Value.Page, _tabs.SelectedTab));
        return entry.Key is null ? Task.FromResult(false) : CloseAsync(entry.Key);
    }

    public async Task<bool> CloseAsync(object screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        if (!_screens.Remove(screen, out var entry)) return false;
        _tabs.TabPages.Remove(entry.Page);
        var closing = CloseCoreAsync(entry);
        _closing.Add(closing);
        try { await closing; }
        finally { _closing.Remove(closing); }
        return true;
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
        foreach (var screen in _screens.Keys.ToArray())
            await CloseAsync(screen);
        await Task.WhenAll(_closing.ToArray());
    }
}
