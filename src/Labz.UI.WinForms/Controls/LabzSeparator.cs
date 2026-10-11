using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

public enum LabzSeparatorOrientation { Horizontal, Vertical }

/// <summary>Theme-aware horizontal or vertical divider.</summary>
[DefaultProperty(nameof(Orientation))]
public class LabzSeparator : Control
{
    private LabzSeparatorOrientation _orientation;
    private int _thickness = 1;

    public LabzSeparator()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        TabStop = false;
        Size = new Size(160, 12);
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(LabzSeparatorOrientation.Horizontal)]
    public LabzSeparatorOrientation Orientation
    {
        get => _orientation;
        set { _orientation = value; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(1)]
    public int Thickness
    {
        get => _thickness;
        set { if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); _thickness = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        using var pen = new Pen(theme.Border, Thickness);
        if (Orientation == LabzSeparatorOrientation.Horizontal)
            e.Graphics.DrawLine(pen, 0, Height / 2f, Width, Height / 2f);
        else
            e.Graphics.DrawLine(pen, Width / 2f, 0, Width / 2f, Height);
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
