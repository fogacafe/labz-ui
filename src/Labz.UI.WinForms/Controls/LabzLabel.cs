using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

public enum LabzLabelVariant { Body, Heading, Caption, Muted }

/// <summary>Theme-aware label with semantic typography variants.</summary>
public class LabzLabel : Label
{
    private LabzLabelVariant _variant;
    private Font? _variantFont;

    public LabzLabel()
    {
        AutoSize = true;
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyVariant();
    }

    [Category("Appearance")]
    [DefaultValue(LabzLabelVariant.Body)]
    public LabzLabelVariant Variant
    {
        get => _variant;
        set { _variant = value; ApplyVariant(); }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_variantFont is null) ApplyVariant();
    }

    private void ApplyVariant()
    {
        _variantFont?.Dispose();
        _variantFont = null;
        var baseFont = Parent?.Font ?? SystemFonts.DefaultFont;
        var size = Variant switch
        {
            LabzLabelVariant.Heading => baseFont.SizeInPoints * 1.35f,
            LabzLabelVariant.Caption => baseFont.SizeInPoints * .85f,
            _ => baseFont.SizeInPoints
        };
        var style = Variant == LabzLabelVariant.Heading ? FontStyle.Bold : FontStyle.Regular;
        _variantFont = new Font(baseFont.FontFamily, size, style);
        Font = _variantFont;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        ForeColor = Variant == LabzLabelVariant.Muted
            ? SystemColors.GrayText : UiThemeManager.Current.Foreground;
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
        if (disposing)
        {
            UiThemeManager.ThemeChanged -= OnThemeChanged;
            _variantFont?.Dispose();
        }
        base.Dispose(disposing);
    }
}
