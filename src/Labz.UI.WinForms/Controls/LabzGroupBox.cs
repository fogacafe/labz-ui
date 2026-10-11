using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

/// <summary>Rounded, themed GroupBox that accepts controls in the WinForms Designer.</summary>
[DefaultProperty(nameof(Text))]
public class LabzGroupBox : GroupBox
{
    private int _cornerRadius = 8;

    public LabzGroupBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Padding = new Padding(12, 20, 12, 12);
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(8)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); _cornerRadius = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        var labelSize = TextRenderer.MeasureText(Text, Font);
        var top = Math.Max(1, labelSize.Height / 2);
        var rectangle = new Rectangle(1, top, Math.Max(1, Width - 3), Math.Max(1, Height - top - 2));
        using var path = RoundedRectangles.Create(rectangle, CornerRadius);
        using var fill = new SolidBrush(theme.Surface);
        using var pen = new Pen(theme.Border);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);
        if (!string.IsNullOrWhiteSpace(Text))
        {
            var labelRect = new Rectangle(12, 0, Math.Min(Width - 24, labelSize.Width + 8), labelSize.Height);
            using var background = new SolidBrush(theme.Surface);
            e.Graphics.FillRectangle(background, labelRect);
            TextRenderer.DrawText(e.Graphics, Text, Font, labelRect, theme.Foreground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
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
