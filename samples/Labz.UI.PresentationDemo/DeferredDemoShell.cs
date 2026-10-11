using Labz.UI.Presentation;

namespace Labz.UI.PresentationDemo;

/// <summary>Demonstrates immediate tab creation, visible loading, and cancellation on close.</summary>
internal sealed class DeferredTradePresenter(IScreenLoadingManager loading) : IAsyncScreenPresenter<TradeForm>
{
    private TradeForm? _view;

    public void Attach(TradeForm view) => _view = view;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        using var first = loading.Begin("Fetching trade");
        using var second = loading.Begin("Fetching counterparties");
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        second.Dispose();
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (_view is { IsDisposed: false } view)
            view.InstanceLabel.Text = "Trade data loaded successfully";
    }

    public Task CloseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class DeferredDemoShell : Form
{
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private readonly ScreenWorkspace _host;
    private readonly Dictionary<DeferredScreenInstance<TradeForm>, string> _titles = new();
    private int _opened;
    private bool _shuttingDown;

    public DeferredDemoShell(ScreenNavigator navigator)
    {
        _host = new ScreenWorkspace(navigator, _tabs);
        _host.CanCloseAsync = ConfirmCloseAsync;
        Text = "Labz UI — navigator and tab host";
        Width = 900;
        Height = 580;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46 };
        var open = new Button { Text = "New independent trade", AutoSize = true };
        var keyed = new Button { Text = "Open/reuse trade #1024", AutoSize = true };
        var positions = new Button { Text = "Open/reuse positions", AutoSize = true };
        var close = new Button { Text = "Close selected", AutoSize = true };
        open.Click += (_, _) => OpenTrade(ScreenOpenPolicy.NewInstance);
        keyed.Click += (_, _) => OpenTrade(ScreenOpenPolicy.ReuseByKey);
        positions.Click += (_, _) => OpenPositions();
        close.Click += async (_, _) => await _host.CloseSelectedAsync();
        toolbar.Controls.Add(open);
        toolbar.Controls.Add(keyed);
        toolbar.Controls.Add(positions);
        toolbar.Controls.Add(close);
        Controls.Add(_tabs);
        Controls.Add(toolbar);
        FormClosing += OnClosing;
        OpenTrade(ScreenOpenPolicy.NewInstance);
        OpenTrade(ScreenOpenPolicy.NewInstance);
    }

    private void OpenTrade(ScreenOpenPolicy policy)
    {
        if (_shuttingDown) return;
        var title = policy == ScreenOpenPolicy.ReuseByKey ? "Trade #1024" : $"Trade #{++_opened}";
        var screen = _host.Open<TradeForm, DeferredTradePresenter>(title + " — loading", policy,
            policy == ScreenOpenPolicy.ReuseByKey ? "trade:1024" : null);
        if (!_titles.TryAdd(screen, title)) return; // Existing keyed tab was selected.
        screen.View.InstanceLabel.Text = "Loading trade data…";
        _ = ObserveInitializationAsync(screen, title);
    }

    private void OpenPositions()
    {
        if (_shuttingDown) return;
        _host.Open<PositionForm, PositionPresenter>("Positions", ScreenOpenPolicy.ReuseByKey, "positions:overview");
    }

    private async Task ObserveInitializationAsync(DeferredScreenInstance<TradeForm> screen, string title)
    {
        try
        {
            await screen.Initialization;
            SetStatus(screen, title + " — ready");
        }
        catch (OperationCanceledException) { /* Expected on close. */ }
        catch (Exception ex)
        {
            SetStatus(screen, title + " — failed");
            if (!screen.View.IsDisposed)
                screen.View.InstanceLabel.Text = $"Loading failed: {ex.Message}";
        }
    }

    private void SetStatus(DeferredScreenInstance<TradeForm> screen, string status)
    {
        if (screen.View.IsDisposed || !_titles.ContainsKey(screen)) return;
        foreach (TabPage page in _tabs.TabPages)
            if (page.Controls.Contains(screen.View)) { page.Text = status; break; }
    }

    private ValueTask<bool> ConfirmCloseAsync(object screen, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (screen is not DeferredScreenInstance<TradeForm> trade ||
            trade.View.IsDisposed || string.IsNullOrWhiteSpace(trade.View.Notes.Text))
            return ValueTask.FromResult(true);

        var choice = MessageBox.Show(this,
            "This trade has unsaved notes. Save before closing?",
            "Unsaved changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

        if (choice == DialogResult.Cancel)
            return ValueTask.FromResult(false);
        if (choice == DialogResult.Yes)
        {
            // Demo only: emulate a successful save. Real apps must await persistence
            // and return false if it fails.
            trade.View.Notes.Clear();
        }
        return ValueTask.FromResult(true);
    }

    private async void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (_shuttingDown) return;
        e.Cancel = true;
        _shuttingDown = true;
        Enabled = false;
        try { await _host.DisposeAsync(); }
        finally
        {
            FormClosing -= OnClosing;
            Close();
        }
    }
}
