using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>
/// Applies the same rounded silhouette to the floating dropdown window and
/// its hosted content. Recalculates when the popup is resized or shown.
/// </summary>
internal sealed class LabzRoundedPopup : IDisposable
{
    private readonly ToolStripDropDown _popup;
    private readonly Control _content;
    private bool _disposed;

    public LabzRoundedPopup(ToolStripDropDown popup, Control content)
    {
        _popup = popup;
        _content = content;
        // The native WinForms dropdown shadow stays rectangular even when Region is rounded.
        // Suppress it so the popup silhouette matches the Labz input chrome.
        _popup.DropShadowEnabled = false;
        // Reserve a physical pixel outside the hosted control: drawing on its
        // Paint event alone is covered by child ListBox/DataGridView windows.
        _popup.Padding = new Padding(1);
        _popup.BackColor = PopupBorderColor();
        _popup.Resize += Refresh;
        _popup.Opened += Refresh;
        _popup.Layout += Refresh;
        _content.Resize += Refresh;

    }

    private void Refresh(object? sender, EventArgs e)
    {
        if (_disposed || _popup.IsDisposed || _content.IsDisposed) return;
        _popup.BackColor = PopupBorderColor();
        var radius = Math.Max(2, UiThemeManager.Current.CornerRadius);
        SetRoundedRegion(_popup, radius);
        SetRoundedRegion(_content, radius);
        _content.Invalidate();
        // ToolStrip can finalize its bounds after Opened; refresh once the layout settles.
        if (sender == _popup && _popup.IsHandleCreated && _popup.Visible)
        {
            _popup.BeginInvoke((Action)(() =>
            {
                if (!_disposed && !_popup.IsDisposed && _popup.Visible)
                {
                    SetRoundedRegion(_popup, Math.Max(2, UiThemeManager.Current.CornerRadius));
                    SetRoundedRegion(_content, Math.Max(2, UiThemeManager.Current.CornerRadius));
                }
            }));
        }
    }

    private static Color PopupBorderColor()
    {
        var theme = UiThemeManager.Current;
        // A subtle outline between the popup surface and the stronger input focus ring.
        const double strength = 0.55;
        return Color.FromArgb(
            (int)Math.Round(theme.Surface.R * (1 - strength) + theme.Border.R * strength),
            (int)Math.Round(theme.Surface.G * (1 - strength) + theme.Border.G * strength),
            (int)Math.Round(theme.Surface.B * (1 - strength) + theme.Border.B * strength));
    }

    private static void SetRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0) return;
        var diameter = Math.Min(radius * 2, Math.Min(control.Width, control.Height));
        using var path = new GraphicsPath();
        var right = control.Width - diameter;
        var bottom = control.Height - diameter;
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(right, 0, diameter, diameter, 270, 90);
        path.AddArc(right, bottom, diameter, diameter, 0, 90);
        path.AddArc(0, bottom, diameter, diameter, 90, 90);
        path.CloseFigure();
        var previous = control.Region;
        control.Region = new Region(path);
        previous?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _popup.Resize -= Refresh;
        _popup.Opened -= Refresh;
        _popup.Layout -= Refresh;
        _content.Resize -= Refresh;

    }
}
