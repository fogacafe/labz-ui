using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;
using Labz.UI.Validation;

namespace Labz.UI.WinForms.Controls;

/// <summary>
/// Designer-friendly text input with native Windows text editing and custom visual chrome.
/// </summary>
[DefaultEvent(nameof(TextChanged))]
[DefaultProperty(nameof(Text))]
public class LabzTextBox : UserControl
{
    private readonly TextBox _editor;
    private readonly Button _searchButton;
    private bool _showSearchButton;
    private ValidationSeverity _validationState;
    private string _validationMessage = string.Empty;
    private bool _hovered;
    private bool _showMessage = true;
    private const int HorizontalPadding = 12;

    public LabzTextBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _editor = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Location = new Point(HorizontalPadding, 10),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _editor.TextChanged += (_, _) => { OnTextChanged(EventArgs.Empty); Invalidate(); };
        _editor.Enter += (_, _) => Invalidate();
        _editor.Leave += (_, _) => Invalidate();
        _editor.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _editor.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        _searchButton = new Button
        {
            Text = "⌕",
            AccessibleName = "Search",
            TabStop = true,
            FlatStyle = FlatStyle.Flat,
            Visible = false,
            Cursor = Cursors.Hand
        };
        _searchButton.FlatAppearance.BorderSize = 0;
        _searchButton.Click += (_, _) => SearchRequested?.Invoke(this, EventArgs.Empty);
        Controls.Add(_searchButton);
        Controls.Add(_editor);
        Size = new Size(240, 42);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        LayoutEditor();
    }

    [Browsable(true)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public override string Text
    {
        get => _editor?.Text ?? string.Empty;
        set { if (_editor is not null) _editor.Text = value; }
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool ShowSearchButton
    {
        get => _showSearchButton;
        set { _showSearchButton = value; _searchButton.Visible = value; LayoutEditor(); Invalidate(); }
    }

    [Category("Action")]
    public event EventHandler? SearchRequested;

    [Category("Appearance")]
    [DefaultValue("")]
    public string PlaceholderText
    {
        get => _editor.PlaceholderText;
        set => _editor.PlaceholderText = value;
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool ReadOnly
    {
        get => _editor.ReadOnly;
        set => _editor.ReadOnly = value;
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool UseSystemPasswordChar
    {
        get => _editor.UseSystemPasswordChar;
        set => _editor.UseSystemPasswordChar = value;
    }

    [Category("Behavior")]
    [DefaultValue(32767)]
    public int MaxLength
    {
        get => _editor.MaxLength;
        set => _editor.MaxLength = value;
    }

    [Category("Validation")]
    [DefaultValue(ValidationSeverity.None)]
    public ValidationSeverity ValidationState
    {
        get => _validationState;
        set { _validationState = value; UpdateLayoutAndPaint(); }
    }

    [Category("Validation")]
    [DefaultValue("")]
    public string ValidationMessage
    {
        get => _validationMessage;
        set { _validationMessage = value ?? string.Empty; UpdateLayoutAndPaint(); }
    }

    [Category("Validation")]
    [DefaultValue(true)]
    public bool ShowValidationMessage
    {
        get => _showMessage;
        set { _showMessage = value; UpdateLayoutAndPaint(); }
    }

    public void ClearValidation()
    {
        _validationState = ValidationSeverity.None;
        _validationMessage = string.Empty;
        UpdateLayoutAndPaint();
    }

    public void ApplyValidation(ValidationMessage? message)
    {
        ValidationState = message?.Severity ?? ValidationSeverity.None;
        ValidationMessage = message?.Message ?? string.Empty;
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        _editor?.Focus();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_editor is not null) { _editor.Font = Font; LayoutEditor(); }
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (_editor is not null) _editor.Enabled = Enabled;
        if (_searchButton is not null) _searchButton.Enabled = Enabled;
        Invalidate();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutEditor();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);

        var hasMessage = _showMessage && !string.IsNullOrWhiteSpace(_validationMessage);
        var inputHeight = Math.Max(28, Height - (hasMessage ? 22 : 0));
        var bounds = new Rectangle(1, 1, Math.Max(1, Width - 3), inputHeight - 3);
        using var path = RoundedRectangles.Create(bounds, theme.CornerRadius);
        using var fill = new SolidBrush(Enabled ? theme.Surface : SystemColors.Control);
        e.Graphics.FillPath(fill, path);

        var borderColor = GetStateColor(theme);
        using var border = new Pen(borderColor, _editor.Focused ? 2 : 1);
        e.Graphics.DrawPath(border, path);

        if (hasMessage)
        {
            var messageBounds = new Rectangle(2, inputHeight + 1, Math.Max(1, Width - 4), 20);
            TextRenderer.DrawText(e.Graphics, _validationMessage, Font, messageBounds, borderColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        }
    }

    private Color GetStateColor(UiTheme theme) => _validationState switch
    {
        ValidationSeverity.Error => theme.Danger,
        ValidationSeverity.Warning => Color.FromArgb(204, 136, 27),
        ValidationSeverity.Success => Color.FromArgb(35, 151, 100),
        _ when _editor.Focused => theme.Primary,
        _ when _hovered => ControlPaint.Dark(theme.Border),
        _ => theme.Border
    };

    private void ApplyTheme()
    {
        if (_editor is null) return;
        _editor.BackColor = UiThemeManager.Current.Surface;
        _editor.ForeColor = UiThemeManager.Current.Foreground;
        _searchButton.BackColor = UiThemeManager.Current.Surface;
        _searchButton.ForeColor = UiThemeManager.Current.Foreground;
        Invalidate();
    }

    private void LayoutEditor()
    {
        if (_editor is null) return;
        var hasMessage = _showMessage && !string.IsNullOrWhiteSpace(_validationMessage);
        var inputHeight = Math.Max(28, Height - (hasMessage ? 22 : 0));
        var searchWidth = _showSearchButton ? 30 : 0;
        _editor.Width = Math.Max(1, Width - HorizontalPadding * 2 - searchWidth);
        if (_searchButton is not null)
        {
            _searchButton.Bounds = new Rectangle(Math.Max(0, Width - HorizontalPadding - 30),
                Math.Max(2, (inputHeight - 28) / 2), 30, 28);
        }
        _editor.Location = new Point(HorizontalPadding, Math.Max(3, (inputHeight - _editor.Height) / 2));
    }

    private void UpdateLayoutAndPaint()
    {
        LayoutEditor();
        Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke((Action)ApplyTheme);
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
