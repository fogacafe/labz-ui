using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware radio button preserving native WinForms grouping and keyboard behavior.</summary>
[DefaultEvent(nameof(CheckedChanged))]
public class LabzRadioButton : RadioButton
{
    public LabzRadioButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AutoSize = true;
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        var y = Math.Max(0, (Height - 16) / 2);
        using var fill = new SolidBrush(theme.Surface);
        using var border = new Pen(Checked ? theme.Primary : theme.Border, 1.5f);
        e.Graphics.FillEllipse(fill, 1, y, 16, 16);
        e.Graphics.DrawEllipse(border, 1, y, 16, 16);
        if (Checked)
        {
            using var dot = new SolidBrush(Enabled ? theme.Primary : SystemColors.ControlDark);
            e.Graphics.FillEllipse(dot, 5, y + 4, 8, 8);
        }
        var textRect = new Rectangle(23, 0, Math.Max(0, Width - 23), Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textRect,
            Enabled ? theme.Foreground : SystemColors.GrayText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, textRect);
    }

    protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

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
