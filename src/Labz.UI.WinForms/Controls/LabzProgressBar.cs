using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

public enum LabzProgressVariant { Primary, Success, Danger }

/// <summary>Theme-aware progress indicator with optional percentage label.</summary>
[DefaultProperty(nameof(Value))]
public class LabzProgressBar : ProgressBar
{
    private LabzProgressVariant _variant;
    private bool _showPercentage;

    public LabzProgressBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(200, 22);
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(LabzProgressVariant.Primary)]
    public LabzProgressVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool ShowPercentage
    {
        get => _showPercentage;
        set { _showPercentage = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        var bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var trackPath = RoundedRectangles.Create(bounds, Math.Min(theme.CornerRadius, bounds.Height / 2));
        using var trackBrush = new SolidBrush(theme.Border);
        e.Graphics.FillPath(trackBrush, trackPath);
        var fraction = Maximum > Minimum ? Math.Clamp((Value - Minimum) / (double)(Maximum - Minimum), 0, 1) : 0;
        var fillWidth = (int)Math.Round(bounds.Width * fraction);
        if (fillWidth > 0)
        {
            var fillRect = new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);
            using var fillPath = RoundedRectangles.Create(fillRect, Math.Min(theme.CornerRadius, Math.Min(fillRect.Width, fillRect.Height) / 2));
            var color = Variant switch
            {
                LabzProgressVariant.Danger => theme.Danger,
                LabzProgressVariant.Success => Color.FromArgb(34, 166, 103),
                _ => theme.Primary
            };
            using var fill = new SolidBrush(Enabled ? color : SystemColors.ControlDark);
            e.Graphics.FillPath(fill, fillPath);
        }
        if (ShowPercentage)
        {
            TextRenderer.DrawText(e.Graphics, $"{fraction:P0}", Font, bounds, theme.Foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    protected override void OnValueChanged(EventArgs e) { base.OnValueChanged(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

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
