using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

public enum LabzButtonVariant { Solid, Outline, Ghost }
public enum LabzButtonSize { Small, Medium, Large }
public enum LabzButtonImagePlacement { Left, Right, Top, Bottom, ImageOnly }

public class LabzButton : Button
{
    private LabzButtonImagePlacement _imagePlacement;
    private int _imageSpacing = 6;
    private Size _imageSize = new(18, 18);
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

    [Category("Appearance")]
    [DefaultValue(LabzButtonImagePlacement.Left)]
    public LabzButtonImagePlacement ImagePlacement
    {
        get => _imagePlacement;
        set { _imagePlacement = value; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(6)]
    public int ImageSpacing
    {
        get => _imageSpacing;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); _imageSpacing = value; Invalidate(); }
    }

    [Category("Appearance")]
    public Size ImageSize
    {
        get => _imageSize;
        set
        {
            if (value.Width <= 0 || value.Height <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            _imageSize = value;
            Invalidate();
        }
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

        DrawContent(e.Graphics, bounds, textColor);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -4, -4),
                theme.Foreground, fill);
        }
    }

    private void DrawContent(Graphics graphics, Rectangle bounds, Color textColor)
    {
        var image = Image;
        if (image is null)
        {
            TextRenderer.DrawText(graphics, Text, Font, bounds, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            return;
        }

        var iconWidth = Math.Min(ImageSize.Width, Math.Max(1, bounds.Width - 8));
        var iconHeight = Math.Min(ImageSize.Height, Math.Max(1, bounds.Height - 8));
        var showText = ImagePlacement != LabzButtonImagePlacement.ImageOnly && !string.IsNullOrEmpty(Text);
        var spacing = showText ? ImageSpacing : 0;
        var textSize = showText ? TextRenderer.MeasureText(graphics, Text, Font,
            new Size(Math.Max(1, bounds.Width - 12), Math.Max(1, bounds.Height - 8)),
            TextFormatFlags.SingleLine) : Size.Empty;
        Rectangle imageRect;
        Rectangle textRect;
        if (ImagePlacement is LabzButtonImagePlacement.Top or LabzButtonImagePlacement.Bottom)
        {
            var totalHeight = iconHeight + spacing + (showText ? textSize.Height : 0);
            var top = bounds.Y + (bounds.Height - totalHeight) / 2;
            var imageFirst = ImagePlacement == LabzButtonImagePlacement.Top;
            imageRect = new Rectangle(bounds.X + (bounds.Width - iconWidth) / 2,
                imageFirst ? top : top + textSize.Height + spacing, iconWidth, iconHeight);
            textRect = new Rectangle(bounds.X + 4, imageFirst ? top + iconHeight + spacing : top,
                Math.Max(0, bounds.Width - 8), textSize.Height);
        }
        else
        {
            var availableText = Math.Max(0, bounds.Width - iconWidth - spacing - 12);
            var measuredText = Math.Min(textSize.Width, availableText);
            var totalWidth = iconWidth + spacing + (showText ? measuredText : 0);
            var left = bounds.X + (bounds.Width - totalWidth) / 2;
            var imageFirst = ImagePlacement != LabzButtonImagePlacement.Right;
            imageRect = new Rectangle(imageFirst ? left : left + measuredText + spacing,
                bounds.Y + (bounds.Height - iconHeight) / 2, iconWidth, iconHeight);
            textRect = new Rectangle(imageFirst ? left + iconWidth + spacing : left,
                bounds.Y, measuredText, bounds.Height);
        }
        graphics.DrawImage(image, imageRect);
        if (showText && textRect.Width > 0 && textRect.Height > 0)
            TextRenderer.DrawText(graphics, Text, Font, textRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
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
