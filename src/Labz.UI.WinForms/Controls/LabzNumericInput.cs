using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

public enum LabzNumericFormat { Number, Currency, Percent }

/// <summary>Theme-painted decimal editor with localized input and custom spinner.</summary>
[DefaultEvent(nameof(ValueChanged))]
public class LabzNumericInput : UserControl
{
    private readonly TextBox _editor = new() { BorderStyle = BorderStyle.None };
    private CultureInfo _culture = CultureInfo.CurrentCulture;
    private decimal _value;
    private decimal _minimum = decimal.MinValue;
    private decimal _maximum = decimal.MaxValue;
    private decimal _increment = 1m;
    private int _decimalPlaces = 2;
    private bool _thousandsSeparator = true;
    private bool _allowEmptyInput;
    private bool _hovered;
    private bool _updating;
    private LabzNumericFormat _numericFormat;

    public LabzNumericInput()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(240, 42);
        TabStop = true;
        Controls.Add(_editor);
        _editor.TextChanged += (_, _) => { if (!_updating) OnTextChanged(EventArgs.Empty); };
        _editor.Enter += (_, _) => Invalidate();
        _editor.Leave += (_, _) => { CommitText(); Invalidate(); };
        _editor.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { CommitText(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Up) { UpButton(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down) { DownButton(); e.SuppressKeyPress = true; }
        };
        _editor.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _editor.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        FormatValue();
        LayoutEditor();
    }

    [Category("Action")]
    public event EventHandler? ValueChanged;

    [Category("Behavior")]
    [DefaultValue(0)]
    public decimal Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (_value != next) { _value = next; ValueChanged?.Invoke(this, EventArgs.Empty); }
            FormatValue();
        }
    }

    [Category("Behavior")]
    public decimal Minimum
    {
        get => _minimum;
        set { _minimum = value; if (_maximum < value) _maximum = value; Value = _value; }
    }

    [Category("Behavior")]
    public decimal Maximum
    {
        get => _maximum;
        set { _maximum = value; if (_minimum > value) _minimum = value; Value = _value; }
    }

    [Category("Behavior")]
    [DefaultValue(1)]
    public decimal Increment
    {
        get => _increment;
        set => _increment = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    [Category("Appearance")]
    [DefaultValue(2)]
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set { _decimalPlaces = Math.Clamp(value, 0, 28); FormatValue(); }
    }

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool ThousandsSeparator
    {
        get => _thousandsSeparator;
        set { _thousandsSeparator = value; FormatValue(); }
    }

    [Category("Appearance")]
    [DefaultValue(LabzNumericFormat.Number)]
    public LabzNumericFormat NumericFormat
    {
        get => _numericFormat;
        set { _numericFormat = value; FormatValue(); }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CultureInfo Culture
    {
        get => _culture;
        set { _culture = value ?? throw new ArgumentNullException(nameof(value)); FormatValue(); }
    }

    [Category("Appearance")]
    [DefaultValue("")]
    public string CultureName
    {
        get => _culture.Name == CultureInfo.CurrentCulture.Name ? "" : _culture.Name;
        set => Culture = string.IsNullOrWhiteSpace(value) ? CultureInfo.CurrentCulture : CultureInfo.GetCultureInfo(value);
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool AllowEmptyInput
    {
        get => _allowEmptyInput;
        set => _allowEmptyInput = value;
    }

    [Browsable(true)]
    public override string Text
    {
        get => _editor?.Text ?? string.Empty;
        set { if (_editor is not null) _editor.Text = value ?? string.Empty; }
    }

    public void UpButton() => Value = Maximum - _value < Increment ? Maximum : _value + Increment;
    public void DownButton() => Value = _value - Minimum < Increment ? Minimum : _value - Increment;

    private void CommitText()
    {
        var text = _editor.Text.Trim();
        if (AllowEmptyInput && text.Length == 0) return;
        if (NumericFormat == LabzNumericFormat.Percent) text = text.Replace("%", "", StringComparison.Ordinal).Trim();
        if (NumericFormat == LabzNumericFormat.Currency) text = text.Replace(_culture.NumberFormat.CurrencySymbol, "", StringComparison.Ordinal).Trim();
        if (decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, _culture, out var parsed))
            Value = parsed;
        else FormatValue();
    }

    private void FormatValue()
    {
        if (_editor is null || _editor.IsDisposed) return;
        var format = NumericFormat switch
        {
            LabzNumericFormat.Currency => "C" + DecimalPlaces,
            LabzNumericFormat.Percent => "N" + DecimalPlaces,
            _ => ThousandsSeparator ? "N" + DecimalPlaces : "F" + DecimalPlaces
        };
        _updating = true;
        try { _editor.Text = _value.ToString(format, _culture) + (NumericFormat == LabzNumericFormat.Percent ? " %" : ""); }
        finally { _updating = false; }
    }

    protected override void OnEnter(EventArgs e) { base.OnEnter(e); _editor.Focus(); }
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_editor is not null) { _editor.Font = Font; LayoutEditor(); }
    }
    protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); LayoutEditor(); }
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (_editor is not null) _editor.Enabled = Enabled;
        Invalidate();
    }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;
        if (e.X >= Width - 34)
        {
            CommitText();
            if (e.Y < Height / 2) UpButton(); else DownButton();
            _editor.Focus();
        }
        else _editor.Focus();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        using var path = RoundedRectangles.Create(new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3)), theme.CornerRadius);
        using var fill = new SolidBrush(theme.Surface);
        using var pen = new Pen(_editor.Focused ? theme.Primary : _hovered ? ControlPaint.Dark(theme.Border) : theme.Border, _editor.Focused ? 2 : 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);
        var x = Width - 19;
        using var arrow = new Pen(Enabled ? theme.Foreground : theme.Border, 1.5f) { LineJoin = LineJoin.Round };
        e.Graphics.DrawLines(arrow, [new Point(x - 4, Height / 2 - 5), new Point(x, Height / 2 - 9), new Point(x + 4, Height / 2 - 5)]);
        e.Graphics.DrawLines(arrow, [new Point(x - 4, Height / 2 + 5), new Point(x, Height / 2 + 9), new Point(x + 4, Height / 2 + 5)]);
    }
    private void LayoutEditor()
    {
        if (_editor is null) return;
        _editor.Bounds = new Rectangle(12, Math.Max(2, (Height - _editor.PreferredHeight) / 2), Math.Max(1, Width - 56), _editor.PreferredHeight);
    }
    private void ApplyTheme()
    {
        if (_editor is null) return;
        var theme = UiThemeManager.Current;
        _editor.BackColor = theme.Surface;
        _editor.ForeColor = theme.Foreground;
        Invalidate();
    }
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            if (!IsHandleCreated) return;
            try { BeginInvoke((Action)ApplyTheme); }
            catch (InvalidOperationException) { }
        }
        else ApplyTheme();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
