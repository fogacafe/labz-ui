using Labz.UI.Presentation;
using Labz.UI.Core.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Labz.UI.PresentationDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var host = CreateDemoHost();
        Application.Run(host);
    }

    public static Form CreateDemoHost()
    {
        var services = new ServiceCollection();
        services.AddLabzScreen<TradeForm, TradePresenter>();
        services.AddScoped<ScreenState>();
        services.AddScoped<IScreenEventBus, ScreenEventBus>();
        services.AddLabzApplicationEventBus();
        services.AddScoped<IScreenLoadingManager, ScreenLoadingManager>();
        services.AddTransient<TradeSelectorControl>();
        services.AddTransient<TradeDetailsControl>();
        services.AddSingleton<ScreenFactory>();
        services.AddTransient<DeferredTradePresenter>();
        services.AddTransient<PositionForm>();
        services.AddTransient<PositionPresenter>();
        services.AddSingleton<DeferredScreenFactory>();
        services.AddTransient<ScreenNavigator>();
        var provider = services.BuildServiceProvider(validateScopes: true);
        try
        {
            var host = new DemoShell(provider.GetRequiredService<ScreenFactory>(), provider.GetRequiredService<DeferredScreenFactory>(), provider.GetRequiredService<ScreenNavigator>());
            host.Disposed += (_, _) => provider.Dispose();
            return host;
        }
        catch
        {
            provider.Dispose();
            throw;
        }
    }
}

internal sealed class ScreenState
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

internal sealed record TradeSelected(int TradeId);

internal sealed class TradeSelectorControl : UserControl
{
    public TradeSelectorControl(IScreenEventBus bus)
    {
        Dock = DockStyle.Top;
        Height = 44;
        var select = new Button { Text = "Select trade #1024", Dock = DockStyle.Fill };
        select.Click += (_, _) => bus.Publish(new TradeSelected(1024));
        Controls.Add(select);
    }
}

internal sealed class TradeDetailsControl : UserControl
{
    private readonly IDisposable _subscription;
    private readonly Label _details = new() { Dock = DockStyle.Fill, Text = "No trade selected" };

    public TradeDetailsControl(IScreenEventBus bus)
    {
        Dock = DockStyle.Top;
        Height = 40;
        Controls.Add(_details);
        _subscription = bus.Subscribe<TradeSelected>(e => _details.Text = $"Selected trade: {e.TradeId}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _subscription.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class TradeForm : Form
{
    private readonly ScreenThemeBinding _themeBinding;
    public Label InstanceLabel { get; } = new() { Dock = DockStyle.Top, Height = 40 };
    public TextBox Notes { get; } = new() { Dock = DockStyle.Top, PlaceholderText = "Independent notes for this tab" };

    public TradeForm(TradeSelectorControl selector, TradeDetailsControl details, IScreenLoadingManager loading)
    {
        Text = "Trade editor";
        Controls.Add(Notes);
        Controls.Add(details);
        Controls.Add(selector);
        Controls.Add(InstanceLabel);
        var indicator = new ScreenLoadingIndicator();
        indicator.Bind(loading);
        Controls.Add(indicator);
        var overlay = new ScreenLoadingOverlay();
        overlay.Bind(loading);
        _themeBinding = new ScreenThemeBinding(overlay, indicator);
        Controls.Add(overlay);
        overlay.BringToFront();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _themeBinding?.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class TradePresenter(ScreenState state) : IScreenPresenter<TradeForm>
{
    public void Attach(TradeForm view)
    {
        view.InstanceLabel.Text = $"Scoped state: {state.InstanceId}";
    }
}

internal sealed class DemoShell : Form
{
    private readonly ScreenFactory _factory;
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private int _opened;

    public DemoShell(ScreenFactory factory, DeferredScreenFactory deferredFactory, ScreenNavigator navigator)
    {
        _factory = factory;
        Text = "Labz UI — independent Form scopes";
        Width = 950;
        Height = 620;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
        var open = new Button { Text = "Open trade editor", AutoSize = true };
        var close = new Button { Text = "Close selected tab", AutoSize = true };
        var deferred = new Button { Text = "Deferred loading demo", AutoSize = true };
        deferred.Click += (_, _) => new DeferredDemoShell(navigator).Show(this);
        open.Click += (_, _) => OpenTrade();
        close.Click += (_, _) => CloseSelected();
        toolbar.Controls.Add(open);
        toolbar.Controls.Add(close);
        toolbar.Controls.Add(deferred);
        Controls.Add(_tabs);
        Controls.Add(toolbar);
        OpenTrade();
        OpenTrade();
    }

    private void OpenTrade()
    {
        var screen = _factory.Open<TradeForm, TradePresenter>();
        var page = new TabPage($"Trade #{++_opened}") { Tag = screen };
        screen.View.TopLevel = false;
        screen.View.FormBorderStyle = FormBorderStyle.None;
        screen.View.Dock = DockStyle.Fill;
        page.Controls.Add(screen.View);
        _tabs.TabPages.Add(page);
        screen.View.Show();
        _tabs.SelectedTab = page;
    }

    private void CloseSelected()
    {
        var page = _tabs.SelectedTab;
        if (page is null) return;
        _tabs.TabPages.Remove(page);
        (page.Tag as IDisposable)?.Dispose();
        page.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (TabPage page in _tabs.TabPages)
                (page.Tag as IDisposable)?.Dispose();
        }
        base.Dispose(disposing);
    }
}
