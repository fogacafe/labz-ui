using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

/// <summary>A themed container surface with configurable rounded corners and border.</summary>
public class LabzPanel : Panel
{
    private bool _showBorder = true;
    private int _cornerRadius = 8;

    public LabzPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Padding = new Padding(12);
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ShowBorder
    {
        get => _showBorder;
        set { _showBorder = value; Invalidate(); }
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
        var rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = RoundedRectangles.Create(rect, CornerRadius);
        using var fill = new SolidBrush(theme.Surface);
        e.Graphics.FillPath(fill, path);
        if (ShowBorder)
        {
            using var border = new Pen(theme.Border);
            e.Graphics.DrawPath(border, path);
        }
        base.OnPaint(e);
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
