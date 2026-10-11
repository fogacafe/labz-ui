using Labz.UI.Presentation;

namespace Labz.UI.PresentationDemo;

/// <summary>A second view type used to verify heterogeneous tabs and scoped state.</summary>
internal sealed class PositionForm : Form
{
    public Label NotificationLabel { get; } = new() { Dock = DockStyle.Top, Height = 35, Text = "No trade save notifications yet" };
    public Label StateLabel { get; } = new() { Dock = DockStyle.Top, Height = 52 };
    public PositionForm()
    {
        Text = "Positions";
        Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Positions overview (demo)\n\nPETR4   1200\nVALE3   800\nITUB4   450",
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        });
        Controls.Add(NotificationLabel);
        Controls.Add(StateLabel);
    }
}

internal sealed class PositionPresenter(ScreenState state, IApplicationEventBus events) : IAsyncScreenPresenter<PositionForm>
{
    private IDisposable? _subscription;
    private PositionsRequest? _request;
    private PositionForm? _view;
    public void SetParameters(PositionsRequest parameters)
    {
        _request = parameters;
        if (_view is not null) _view.StateLabel.Text = $"Portfolio: {parameters.Portfolio} — scope: {state.InstanceId}";
    }
    public void Attach(PositionForm view)
    {
        _view = view;
        view.StateLabel.Text = $"Position scope: {state.InstanceId}";
        _subscription = events.Subscribe<DemoTradeSaved>(e =>
        {
            if (!view.IsDisposed) view.NotificationLabel.Text = $"Trade #{e.TradeId} saved — positions notified";
        });
    }
    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_request is null) throw new InvalidOperationException("Positions parameters were not provided.");
        return Task.CompletedTask;
    }
    public Task CloseAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }
}
