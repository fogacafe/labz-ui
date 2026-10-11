using Labz.UI.Presentation;
using Microsoft.Extensions.DependencyInjection;

namespace Labz.UI.PresentationDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var services = new ServiceCollection();
        services.AddLabzScreen<TradeForm, TradePresenter>();
        services.AddScoped<ScreenState>();
        services.AddScoped<IScreenEventBus, ScreenEventBus>();
        services.AddSingleton<ScreenFactory>();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        Application.Run(new DemoShell(provider.GetRequiredService<ScreenFactory>()));
    }
}

internal sealed class ScreenState
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

internal sealed class TradeForm : Form
{
    public Label InstanceLabel { get; } = new() { Dock = DockStyle.Top, Height = 40 };
    public TextBox Notes { get; } = new() { Dock = DockStyle.Top, PlaceholderText = "Independent notes for this tab" };

    public TradeForm()
    {
        Text = "Trade editor";
        Controls.Add(Notes);
        Controls.Add(InstanceLabel);
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

    public DemoShell(ScreenFactory factory)
    {
        _factory = factory;
        Text = "Labz UI — independent Form scopes";
        Width = 950;
        Height = 620;
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
        var open = new Button { Text = "Open trade editor", AutoSize = true };
        var close = new Button { Text = "Close selected tab", AutoSize = true };
        open.Click += (_, _) => OpenTrade();
        close.Click += (_, _) => CloseSelected();
        toolbar.Controls.Add(open);
        toolbar.Controls.Add(close);
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
