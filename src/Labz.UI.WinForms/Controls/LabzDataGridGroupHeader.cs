using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>A compact clickable header for a locally grouped LabzDataGrid.</summary>
[DefaultEvent(nameof(ExpandToggleRequested))]
public sealed class LabzDataGridGroupHeader : UserControl
{
    private readonly Button _toggle;
    private readonly Label _title;
    private readonly Label _summary;
    private bool _expanded = true;
    private readonly Font _titleFont;

    public event EventHandler? ExpandToggleRequested;

    [Category("Appearance")]
    public string GroupTitle
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    [Category("Appearance")]
    public string GroupSummary
    {
        get => _summary.Text;
        set => _summary.Text = value;
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool Expanded
    {
        get => _expanded;
        set
        {
            _expanded = value;
            _toggle.Text = value ? "▾" : "▸";
            AccessibleDescription = value ? "Expanded group" : "Collapsed group";
        }
    }

    public LabzDataGridGroupHeader()
    {
        Height = 40;
        MinimumSize = new Size(120, 40);
        _toggle = new Button
        {
            Dock = DockStyle.Left,
            Width = 36,
            FlatStyle = FlatStyle.Flat,
            TabStop = true,
            AccessibleName = "Toggle group"
        };
        _toggle.FlatAppearance.BorderSize = 0;
        _titleFont = new Font(Font, FontStyle.Bold);
        _title = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Font = _titleFont
        };
        _summary = new Label
        {
            Dock = DockStyle.Right,
            Width = 180,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true
        };
        Controls.Add(_title);
        Controls.Add(_summary);
        Controls.Add(_toggle);
        _toggle.Click += (_, _) => ExpandToggleRequested?.Invoke(this, EventArgs.Empty);
        _title.Click += (_, _) => ExpandToggleRequested?.Invoke(this, EventArgs.Empty);
        _summary.Click += (_, _) => ExpandToggleRequested?.Invoke(this, EventArgs.Empty);
        Expanded = true;
        ApplyTheme();
        UiThemeManager.ThemeChanged += ThemeChanged;
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        ForeColor = theme.Foreground;
        _title.BackColor = theme.Background;
        _title.ForeColor = theme.Foreground;
        _summary.BackColor = theme.Background;
        _summary.ForeColor = theme.Foreground;
        _toggle.BackColor = theme.Background;
        _toggle.ForeColor = theme.Foreground;
    }

    private void ThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke((Action)ApplyTheme);
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UiThemeManager.ThemeChanged -= ThemeChanged;
            _titleFont.Dispose();
        }
        base.Dispose(disposing);
    }
}
