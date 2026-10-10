using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

public enum LabzButtonVariant { Solid, Outline, Ghost }
public enum LabzButtonSize { Small, Medium, Large }

public class LabzButton : Button
{
    private bool _hovered;
    private bool _pressed;
    private LabzButtonVariant _variant;
    private LabzButtonSize _buttonSize = LabzButtonSize.Medium;

    public LabzButton()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        ApplySize();
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [DefaultValue(LabzButtonVariant.Solid)]
    public LabzButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    [DefaultValue(LabzButtonSize.Medium)]
    public LabzButtonSize ButtonSize
    {
        get => _buttonSize;
        set { _buttonSize = value; ApplySize(); Invalidate(); }
    }

    // Compatibility with the first experimental button API.
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Outline
    {
        get => Variant == LabzButtonVariant.Outline;
        set => Variant = value ? LabzButtonVariant.Outline : LabzButtonVariant.Solid;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hovered = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && Enabled)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);

        var bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var path = RoundedRectangles.Create(bounds, theme.CornerRadius);

        var baseColor = Variant == LabzButtonVariant.Solid ? theme.Primary : theme.Surface;
        var fill = !Enabled ? Color.FromArgb(110, 115, 125)
            : _pressed ? ControlPaint.Dark(baseColor, .12f)
            : _hovered ? ControlPaint.Light(baseColor, .12f)
            : baseColor;

        if (Variant != LabzButtonVariant.Ghost || _hovered || _pressed)
        {
            using var brush = new SolidBrush(fill);
            e.Graphics.FillPath(brush, path);
        }

        if (Variant == LabzButtonVariant.Outline)
        {
            using var pen = new Pen(Enabled ? theme.Border : SystemColors.ControlDark);
            e.Graphics.DrawPath(pen, path);
        }

        var textColor = !Enabled ? Color.LightGray
            : Variant == LabzButtonVariant.Solid ? Color.White : theme.Foreground;

        TextRenderer.DrawText(e.Graphics, Text, Font, bounds, textColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4),
                theme.Foreground, fill);
        }
    }

    private void ApplySize()
    {
        Size = ButtonSize switch
        {
            LabzButtonSize.Small => new Size(104, 30),
            LabzButtonSize.Large => new Size(156, 46),
            _ => new Size(130, 38)
        };
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
