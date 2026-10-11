using System.ComponentModel;
using System.Globalization;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

public enum LabzNumericFormat { Number, Currency, Percent }

/// <summary>Decimal-based numeric editor with native spin/keyboard semantics and localized formatting.</summary>
public class LabzNumericInput : NumericUpDown
{
    private LabzNumericFormat _numericFormat;
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private bool _updatingText;
    private bool _allowEmptyInput;

    public LabzNumericInput()
    {
        DecimalPlaces = 2;
        ThousandsSeparator = true;
        Maximum = decimal.MaxValue;
        Minimum = decimal.MinValue;
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    [Category("Appearance")]
    [DefaultValue(LabzNumericFormat.Number)]
    public LabzNumericFormat NumericFormat
    {
        get => _numericFormat;
        set
        {
            _numericFormat = value;
            UpdateEditText();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            _culture = value ?? throw new ArgumentNullException(nameof(value));
            UpdateEditText();
        }
    }

    [Category("Appearance")]
    [DefaultValue("")]
    public string CultureName
    {
        get => _culture.Name == CultureInfo.CurrentCulture.Name ? "" : _culture.Name;
        set => Culture = string.IsNullOrWhiteSpace(value) ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(value);
    }

    /// <summary>When enabled, blank text is left untouched until the user enters a value; Value remains the last committed decimal.</summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool AllowEmptyInput
    {
        get => _allowEmptyInput;
        set => _allowEmptyInput = value;
    }

    /// <summary>For percent format, 25 means 25%, not 0.25.</summary>
    protected override void UpdateEditText()
    {
        if (IsDisposed) return;
        var format = NumericFormat switch
        {
            LabzNumericFormat.Currency => "C" + DecimalPlaces,
            LabzNumericFormat.Percent => "N" + DecimalPlaces,
            _ => ThousandsSeparator ? "N" + DecimalPlaces : "F" + DecimalPlaces
        };
        if (_updatingText) return;
        _updatingText = true;
        try
        {
            var text = base.Value.ToString(format, _culture ?? CultureInfo.CurrentCulture);
            Text = NumericFormat == LabzNumericFormat.Percent ? text + " %" : text;
        }
        finally { _updatingText = false; }
    }

    protected override void ValidateEditText()
    {
        if (_updatingText) return;
        var text = Text.Trim();
        if (AllowEmptyInput && text.Length == 0) return;
        if (NumericFormat == LabzNumericFormat.Percent)
            text = text.Replace("%", "", StringComparison.Ordinal).Trim();
        if (NumericFormat == LabzNumericFormat.Currency)
            text = text.Replace(_culture.NumberFormat.CurrencySymbol, "", StringComparison.Ordinal).Trim();

        var styles = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
        if (decimal.TryParse(text, styles, _culture, out var parsed))
            Value = Math.Clamp(parsed, Minimum, Maximum);
        UpdateEditText();
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        ForeColor = theme.Foreground;
        Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            if (!IsHandleCreated) return;
            try { BeginInvoke((Action)ApplyTheme); }
            catch (InvalidOperationException) { /* Closing handle. */ }
        }
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
