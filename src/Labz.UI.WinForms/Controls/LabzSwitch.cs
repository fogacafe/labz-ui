using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Keyboard-accessible on/off toggle with WinForms data-binding support.</summary>
[DefaultProperty(nameof(Checked))]
[DefaultEvent(nameof(CheckedChanged))]
public class LabzSwitch : CheckBox
{
    public LabzSwitch()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        AutoSize = false;
        Size = new Size(52, 28);
        AccessibleRole = AccessibleRole.CheckButton;
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        var track = new Rectangle(2, Math.Max(2, (Height - 24) / 2), 44, 24);
        using var path = new GraphicsPath();
        path.AddArc(track.X, track.Y, 24, 24, 90, 180);
        path.AddArc(track.Right - 24, track.Y, 24, 24, 270, 180);
        path.CloseFigure();
        using var fill = new SolidBrush(Checked && Enabled ? theme.Primary : theme.Border);
        e.Graphics.FillPath(fill, path);
        var knobX = Checked ? track.Right - 21 : track.Left + 3;
        using var knob = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(knob, knobX, track.Y + 3, 18, 18);
        if (Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(0, 0, 49, Height));
        if (!string.IsNullOrWhiteSpace(Text))
        {
            var textRect = new Rectangle(54, 0, Math.Max(0, Width - 54), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textRect,
                Enabled ? theme.Foreground : SystemColors.GrayText,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
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
