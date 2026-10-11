using System.Drawing;
using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>
/// Optional blocking loading layer for one Form or UserControl.
/// Add it as the last child of the target container and call BringToFront.
/// </summary>
public sealed class ScreenLoadingOverlay : UserControl
{
    private readonly Label _message = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 12, FontStyle.Regular)
    };
    private readonly ProgressBar _progress = new()
    {
        Dock = DockStyle.Bottom,
        Height = 5,
        Style = ProgressBarStyle.Marquee,
        MarqueeAnimationSpeed = 24
    };
    private IScreenLoadingManager? _manager;

    public ScreenLoadingOverlay()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 246, 249);
        Controls.Add(_message);
        Controls.Add(_progress);
        Visible = false;
    }

    public void Bind(IScreenLoadingManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (ReferenceEquals(_manager, manager)) return;
        if (_manager is not null) _manager.Changed -= OnChanged;
        _manager = manager;
        _manager.Changed += OnChanged;
        RefreshState();
    }

    private void OnChanged(object? sender, EventArgs args)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            if (!IsHandleCreated) return;
            try { BeginInvoke((Action)RefreshState); }
            catch (InvalidOperationException) { /* Closing handle. */ }
            return;
        }
        RefreshState();
    }

    private void RefreshState()
    {
        if (IsDisposed || Disposing || _manager is null) return;
        var loading = _manager.IsLoading;
        _message.Text = loading
            ? (_manager.Message ?? "Loading…") + $" ({_manager.ActiveCount})"
            : string.Empty;
        Visible = loading;
        if (loading) BringToFront();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _manager is not null)
        {
            _manager.Changed -= OnChanged;
            _manager = null;
        }
        base.Dispose(disposing);
    }
}
