using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

public enum LabzBadgeVariant { Neutral, Primary, Success, Warning, Error }

/// <summary>Compact status indicator with semantic colors and theme-aware rendering.</summary>
public class LabzBadge : Control
{
    private LabzBadgeVariant _variant;
    public LabzBadge()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(96, 28);
        Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold);
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(LabzBadgeVariant.Neutral)]
    public LabzBadgeVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var color = Variant switch
        {
            LabzBadgeVariant.Primary => theme.Primary,
            LabzBadgeVariant.Success => Color.ForestGreen,
            LabzBadgeVariant.Warning => Color.DarkOrange,
            LabzBadgeVariant.Error => theme.Danger,
            _ => theme.Border
        };
        var rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = RoundedRectangles.Create(rect, Height / 2);
        using var fill = new SolidBrush(color);
        e.Graphics.FillPath(fill, path);
        TextRenderer.DrawText(e.Graphics, Text, Font, rect,
            Variant == LabzBadgeVariant.Neutral ? theme.Foreground : Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke((Action)Invalidate);
        else Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
