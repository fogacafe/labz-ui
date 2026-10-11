using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

public enum LabzImageFit { Contain, Cover, Fill, Center, Original }

/// <summary>Image viewer with proportional fitting, clipping, opacity and optional themed border.</summary>
[DefaultProperty(nameof(Image))]
public class LabzImage : Control
{
    private Image? _image;
    private LabzImageFit _fit = LabzImageFit.Contain;
    private int _cornerRadius = 8;
    private bool _showBorder;
    private float _opacity = 1f;

    public LabzImage()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(160, 120);
        TabStop = false;
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(null)]
    public Image? Image
    {
        get => _image;
        set { _image = value; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(LabzImageFit.Contain)]
    public LabzImageFit Fit
    {
        get => _fit;
        set { _fit = value; Invalidate(); }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
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

    /// <summary>Image opacity from 0 (transparent) to 1 (opaque).</summary>
    [Category("Appearance")]
    [DefaultValue(1f)]
    public float Opacity
    {
        get => _opacity;
        set
        {
            if (!float.IsFinite(value) || value < 0 || value > 1)
                throw new ArgumentOutOfRangeException(nameof(value));
            _opacity = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        var bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using var shape = RoundedRectangles.Create(bounds, CornerRadius);
        using var background = new SolidBrush(theme.Surface);
        e.Graphics.FillPath(background, shape);

        if (Image is { Width: > 0, Height: > 0 } image && Opacity > 0)
        {
            var state = e.Graphics.Save();
            try
            {
                e.Graphics.SetClip(shape);
                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                var destination = CalculateImageBounds(image.Size, bounds, Fit);
                using var attributes = new ImageAttributes();
                var matrix = new ColorMatrix { Matrix33 = Opacity };
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                e.Graphics.DrawImage(image, destination, 0, 0, image.Width, image.Height,
                    GraphicsUnit.Pixel, attributes);
            }
            finally { e.Graphics.Restore(state); }
        }

        if (ShowBorder)
        {
            using var pen = new Pen(theme.Border);
            e.Graphics.DrawPath(pen, shape);
        }
    }

    public static Rectangle CalculateImageBounds(Size imageSize, Rectangle bounds, LabzImageFit fit)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0) return Rectangle.Empty;
        if (fit == LabzImageFit.Fill) return bounds;
        if (fit == LabzImageFit.Original)
            return new Rectangle(bounds.X + (bounds.Width - imageSize.Width) / 2,
                bounds.Y + (bounds.Height - imageSize.Height) / 2, imageSize.Width, imageSize.Height);
        var scale = fit switch
        {
            LabzImageFit.Cover => Math.Max(bounds.Width / (double)imageSize.Width,
                bounds.Height / (double)imageSize.Height),
            LabzImageFit.Center => Math.Min(1d, Math.Min(bounds.Width / (double)imageSize.Width,
                bounds.Height / (double)imageSize.Height)),
            _ => Math.Min(bounds.Width / (double)imageSize.Width,
                bounds.Height / (double)imageSize.Height)
        };
        var width = Math.Max(1, (int)Math.Round(imageSize.Width * scale));
        var height = Math.Max(1, (int)Math.Round(imageSize.Height * scale));
        return new Rectangle(bounds.X + (bounds.Width - width) / 2,
            bounds.Y + (bounds.Height - height) / 2, width, height);
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
        // Image ownership stays with the caller, matching PictureBox semantics.
        base.Dispose(disposing);
    }
}
