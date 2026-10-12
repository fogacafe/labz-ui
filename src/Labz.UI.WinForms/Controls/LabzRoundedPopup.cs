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
        _popup.Resize += Refresh;
        _popup.Opened += Refresh;
        _popup.Layout += Refresh;
        _content.Resize += Refresh;
        _content.Paint += PaintBorder;
        _popup.Paint += PaintPopupBorder;
    }

    private void Refresh(object? sender, EventArgs e)
    {
        if (_disposed || _popup.IsDisposed || _content.IsDisposed) return;
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

    private void PaintBorder(object? sender, PaintEventArgs e)
    {
        if (_disposed || _content.Width < 3 || _content.Height < 3) return;
        DrawBorder(e.Graphics, _content.ClientRectangle);
    }

    private void PaintPopupBorder(object? sender, PaintEventArgs e)
    {
        if (_disposed || _popup.Width < 3 || _popup.Height < 3) return;
        DrawBorder(e.Graphics, _popup.ClientRectangle);
    }

    private static void DrawBorder(Graphics graphics, Rectangle bounds)
    {
        var theme = UiThemeManager.Current;
        var radius = Math.Max(2, theme.CornerRadius);
        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width - 2, bounds.Height - 2));
        if (diameter < 2) return;
        var x = bounds.Left + 1;
        var y = bounds.Top + 1;
        var right = bounds.Right - 2 - diameter;
        var bottom = bounds.Bottom - 2 - diameter;
        using var path = new GraphicsPath();
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(right, y, diameter, diameter, 270, 90);
        path.AddArc(right, bottom, diameter, diameter, 0, 90);
        path.AddArc(x, bottom, diameter, diameter, 90, 90);
        path.CloseFigure();
        using var pen = new Pen(theme.Border, 1);
        var previous = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawPath(pen, path);
        graphics.SmoothingMode = previous;
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
        _content.Paint -= PaintBorder;
        _popup.Paint -= PaintPopupBorder;
    }
}
