using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Expandable panel with a keyboard-accessible header and designer-friendly content surface.</summary>
[DefaultProperty(nameof(Title))]
[DefaultEvent(nameof(ExpandedChanged))]
public class LabzAccordion : UserControl
{
    private readonly Button _header = new()
    {
        Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleLeft,
        FlatStyle = FlatStyle.Flat, TabStop = true, AccessibleName = "Toggle section"
    };
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new Padding(12) };
    private bool _expanded = true;
    private int _expandedHeight = 180;
    private bool _updating;

    public LabzAccordion()
    {
        SetStyle(ControlStyles.ResizeRedraw, true);
        _header.FlatAppearance.BorderSize = 0;
        _header.Click += (_, _) => Expanded = !Expanded;
        Controls.Add(_content);
        Controls.Add(_header);
        Height = _expandedHeight;
        MinimumSize = new Size(0, 40);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        UpdateHeader();
    }

    /// <summary>Place section controls inside ContentPanel (also available in the Designer).</summary>
    [Category("Layout")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Panel ContentPanel => _content;

    [Category("Appearance")]
    [DefaultValue("")]
    public string Title
    {
        get => _title;
        set { _title = value ?? ""; UpdateHeader(); }
    }
    private string _title = "";

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_expanded == value) return;
            if (_expanded && Height > _header.Height) _expandedHeight = Height;
            _expanded = value;
            _updating = true;
            try
            {
                _content.Visible = value;
                Height = value ? Math.Max(_header.Height + 1, _expandedHeight) : _header.Height;
            }
            finally { _updating = false; }
            UpdateHeader();
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Category("Layout")]
    [DefaultValue(180)]
    public int ExpandedHeight
    {
        get => _expandedHeight;
        set
        {
            if (value <= _header.Height) throw new ArgumentOutOfRangeException(nameof(value));
            _expandedHeight = value;
            if (Expanded) Height = value;
        }
    }

    public event EventHandler? ExpandedChanged;

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (!_updating && _expanded && Height > _header.Height) _expandedHeight = Height;
    }

    private void UpdateHeader()
    {
        if (_header is null) return;
        _header.Text = (_expanded ? "▾  " : "▸  ") + Title;
        _header.AccessibleName = string.IsNullOrEmpty(Title) ? "Toggle section" : "Toggle " + Title;
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        _header.BackColor = theme.Surface;
        _header.ForeColor = theme.Foreground;
        _content.BackColor = theme.Surface;
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
