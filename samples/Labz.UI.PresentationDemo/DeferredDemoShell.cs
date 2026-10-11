using Labz.UI.Presentation;

namespace Labz.UI.PresentationDemo;

/// <summary>Demonstrates immediate tab creation, visible loading, and cancellation on close.</summary>
internal sealed class DeferredTradePresenter : IAsyncScreenPresenter<TradeForm>
{
    private TradeForm? _view;

    public void Attach(TradeForm view) => _view = view;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (_view is { IsDisposed: false } view)
            view.InstanceLabel.Text = "Trade data loaded successfully";
    }

    public Task CloseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class DeferredDemoShell : Form
{
    private readonly DeferredScreenFactory _factory;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly Dictionary<TabPage, DeferredScreenInstance<TradeForm>> _screens = new();
    private readonly HashSet<Task> _closing = new();
    private int _opened;
    private bool _shuttingDown;

    public DeferredDemoShell(DeferredScreenFactory factory)
    {
        _factory = factory;
        Text = "Labz UI — deferred initialization";
        Width = 900;
        Height = 580;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46 };
        var open = new Button { Text = "Open loading trade", AutoSize = true };
        var close = new Button { Text = "Close selected", AutoSize = true };
        open.Click += (_, _) => OpenTrade();
        close.Click += async (_, _) => await CloseSelectedAsync();
        toolbar.Controls.Add(open);
        toolbar.Controls.Add(close);
        Controls.Add(_tabs);
        Controls.Add(toolbar);
        FormClosing += OnClosing;
        OpenTrade();
        OpenTrade();
    }

    private void OpenTrade()
    {
        if (_shuttingDown) return;
        var screen = _factory.Open<TradeForm, DeferredTradePresenter>();
        var page = new TabPage($"Trade #{++_opened} — loading");
        _screens.Add(page, screen);
        screen.View.TopLevel = false;
        screen.View.FormBorderStyle = FormBorderStyle.None;
        screen.View.Dock = DockStyle.Fill;
        screen.View.InstanceLabel.Text = "Loading trade data…";
        page.Controls.Add(screen.View);
        _tabs.TabPages.Add(page);
        screen.View.Show();
        _tabs.SelectedTab = page;
        _ = ObserveInitializationAsync(page, screen);
    }

    private async Task ObserveInitializationAsync(TabPage page, DeferredScreenInstance<TradeForm> screen)
    {
        try
        {
            await screen.Initialization;
            if (!page.IsDisposed && _screens.ContainsKey(page))
                page.Text = page.Text.Replace(" — loading", " — ready");
        }
        catch (OperationCanceledException)
        {
            // Closing the tab intentionally cancels its initialization.
        }
        catch (Exception ex)
        {
            if (!page.IsDisposed && _screens.ContainsKey(page))
            {
                page.Text = page.Text.Replace(" — loading", " — failed");
                screen.View.InstanceLabel.Text = $"Loading failed: {ex.Message}";
            }
        }
    }

    private async Task CloseSelectedAsync()
    {
        if (_tabs.SelectedTab is { } page)
            await ClosePageAsync(page);
    }

    private async Task ClosePageAsync(TabPage page)
    {
        if (!_screens.Remove(page, out var screen)) return;
        _tabs.TabPages.Remove(page);
        var close = CloseAndDisposeAsync(screen, page);
        _closing.Add(close);
        try { await close; }
        finally { _closing.Remove(close); }
    }

    private static async Task CloseAndDisposeAsync(
        DeferredScreenInstance<TradeForm> screen, TabPage page)
    {
        try { await screen.DisposeAsync(); }
        finally { page.Dispose(); }
    }

    private async void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (_shuttingDown) return;
        e.Cancel = true;
        _shuttingDown = true;
        Enabled = false;
        try
        {
            foreach (var page in _screens.Keys.ToArray())
                await ClosePageAsync(page);
            await Task.WhenAll(_closing.ToArray());
        }
        finally
        {
            FormClosing -= OnClosing;
            Close();
        }
    }
}
