using System.ComponentModel;
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
    private readonly ScreenSpinner _spinner = new() { Anchor = AnchorStyles.None };
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private IScreenLoadingManager? _manager;

    /// <summary>Apply explicit light/dark colors without coupling to a theme provider.</summary>
    public void ApplyPalette(Color background, Color foreground, Color accent)
    {
        BackColor = background;
        _content.BackColor = background;
        _message.BackColor = background;
        _message.ForeColor = foreground;
        _spinner.BackColor = background;
        _spinner.ForeColor = accent;
        _spinner.Invalidate();
    }

    public void UseDarkPalette() => ApplyPalette(
        Color.FromArgb(29, 32, 39), Color.FromArgb(237, 239, 244), Color.FromArgb(127, 165, 255));

    public void UseLightPalette() => ApplyPalette(
        Color.FromArgb(245, 246, 249), Color.FromArgb(38, 43, 53), Color.FromArgb(70, 110, 205));

    public ScreenLoadingOverlay()
    {
        Dock = DockStyle.Fill;
        UseLightPalette();
        _content.Controls.Add(_message);
        _content.Controls.Add(_spinner);
        Controls.Add(_content);
        _content.Resize += (_, _) =>
        {
            _spinner.Location = new Point((_content.Width - _spinner.Width) / 2,
                Math.Max(0, (_content.Height - _spinner.Height) / 2 - 35));
        };
        _message.Padding = new Padding(0, 75, 0, 0);
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
        _spinner.Animating = loading;
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
