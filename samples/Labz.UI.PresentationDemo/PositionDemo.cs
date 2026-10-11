using Labz.UI.Presentation;

namespace Labz.UI.PresentationDemo;

/// <summary>A second view type used to verify heterogeneous tabs and scoped state.</summary>
internal sealed class PositionForm : Form
{
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
        Controls.Add(StateLabel);
    }
}

internal sealed class PositionPresenter(ScreenState state) : IAsyncScreenPresenter<PositionForm>
{
    public void Attach(PositionForm view) => view.StateLabel.Text = $"Position scope: {state.InstanceId}";
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task CloseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
