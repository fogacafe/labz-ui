using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware checkbox preserving native WinForms checked state and events.</summary>
[DefaultEvent(nameof(CheckedChanged))]
public class LabzCheckBox : CheckBox
{
    public LabzCheckBox()
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
        var side = 16;
        var y = Math.Max(0, (Height - side) / 2);
        var box = new Rectangle(1, y, side, side);
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(box, new Size(4, 4));
        using var fill = new SolidBrush(Checked && CheckState != CheckState.Unchecked ? theme.Primary : theme.Surface);
        using var border = new Pen(Enabled ? theme.Border : SystemColors.ControlDark);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        if (CheckState == CheckState.Checked)
        {
            using var pen = new Pen(Color.White, 2f);
            e.Graphics.DrawLines(pen, [new Point(4, y + 8), new Point(7, y + 11), new Point(13, y + 5)]);
        }
        else if (CheckState == CheckState.Indeterminate)
        {
            using var pen = new Pen(Color.White, 2f);
            e.Graphics.DrawLine(pen, 4, y + 8, 13, y + 8);
        }
        var textRect = new Rectangle(23, 0, Math.Max(0, Width - 23), Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textRect,
            Enabled ? theme.Foreground : SystemColors.GrayText,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, textRect);
    }

    protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
    protected override void OnCheckStateChanged(EventArgs e) { base.OnCheckStateChanged(e); Invalidate(); }
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
