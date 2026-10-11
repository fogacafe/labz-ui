using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>
/// Native ComboBox selection with Labz theming and an optional external search action.
/// SearchRequested deliberately does not fetch data; consumers own their data source.
/// </summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
[DefaultProperty(nameof(Items))]
public class LabzComboBox : UserControl
{
    private readonly ComboBox _combo;
    private readonly Button _searchButton;
    private bool _showSearchButton;
    private readonly Panel _frame = new() { Dock = DockStyle.Fill, Padding = new Padding(1) };

    public LabzComboBox()
    {
        _combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            IntegralHeight = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 25,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _combo.DrawItem += DrawComboItem;
        _combo.SelectedIndexChanged += (_, e) => SelectedIndexChanged?.Invoke(this, e);
        _combo.TextChanged += (_, _) => OnTextChanged(EventArgs.Empty);
        _searchButton = new Button
        {
            Text = "⌕",
            AccessibleName = "Search",
            FlatStyle = FlatStyle.Flat,
            TabStop = true,
            Cursor = Cursors.Hand,
            Visible = false
        };
        _searchButton.FlatAppearance.BorderSize = 0;
        _searchButton.Click += (_, _) => SearchRequested?.Invoke(this, EventArgs.Empty);
        _combo.FlatStyle = FlatStyle.Flat;
        _combo.Dock = DockStyle.None;
        Controls.Add(_frame);
        _frame.Controls.Add(_combo);
        _frame.Controls.Add(_searchButton);
        Size = new Size(240, 42);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        LayoutChildren();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("Data")]
    public ComboBox.ObjectCollection Items => _combo.Items;

    [Category("Data")]
    [DefaultValue(null)]
    public object? DataSource
    {
        get => _combo.DataSource;
        set => _combo.DataSource = value;
    }

    [Category("Data")]
    [DefaultValue("")]
    public string DisplayMember
    {
        get => _combo.DisplayMember;
        set => _combo.DisplayMember = value;
    }

    [Category("Data")]
    [DefaultValue("")]
    public string ValueMember
    {
        get => _combo.ValueMember;
        set => _combo.ValueMember = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? SelectedItem
    {
        get => _combo.SelectedItem;
        set => _combo.SelectedItem = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object? SelectedValue
    {
        get => _combo.SelectedValue;
        set => _combo.SelectedValue = value;
    }

    [Category("Behavior")]
    [DefaultValue(-1)]
    public int SelectedIndex
    {
        get => _combo.SelectedIndex;
        set => _combo.SelectedIndex = value;
    }

    [Category("Behavior")]
    [DefaultValue(ComboBoxStyle.DropDownList)]
    public ComboBoxStyle DropDownStyle
    {
        get => _combo.DropDownStyle;
        set => _combo.DropDownStyle = value;
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool ShowSearchButton
    {
        get => _showSearchButton;
        set
        {
            _showSearchButton = value;
            _searchButton.Visible = value;
            LayoutChildren();
        }
    }

    [Category("Action")]
    public event EventHandler? SearchRequested;

    public event EventHandler? SelectedIndexChanged;

    public override string Text
    {
        get => _combo?.Text ?? string.Empty;
        set { if (_combo is not null) _combo.Text = value; }
    }

    public void BeginUpdate() => _combo.BeginUpdate();
    public void EndUpdate() => _combo.EndUpdate();

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        _combo?.Focus();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutChildren();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_combo is not null) _combo.Font = Font;
        LayoutChildren();
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (_combo is not null) _combo.Enabled = Enabled;
        if (_searchButton is not null) _searchButton.Enabled = Enabled;
    }

    private void DrawComboItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _combo.Items.Count) return;
        var theme = UiThemeManager.Current;
        var selected = (e.State & DrawItemState.Selected) != 0;
        var background = selected ? theme.Primary : theme.Surface;
        var foreground = selected ? Color.White : theme.Foreground;
        using var brush = new SolidBrush(background);
        e.Graphics.FillRectangle(brush, e.Bounds);
        var label = _combo.GetItemText(_combo.Items[e.Index]);
        TextRenderer.DrawText(e.Graphics, label, e.Font ?? Font,
            new Rectangle(e.Bounds.X + 8, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 12), e.Bounds.Height),
            foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        if ((e.State & DrawItemState.Focus) != 0) e.DrawFocusRectangle();
    }

    private void LayoutChildren()
    {
        if (_combo is null || _searchButton is null) return;
        var actionWidth = _showSearchButton ? 36 : 0;
        _combo.Bounds = new Rectangle(5, Math.Max(2, (Height - _combo.Height) / 2),
            Math.Max(1, Width - actionWidth - 10), _combo.Height);
        _searchButton.Bounds = new Rectangle(Math.Max(0, Width - 36),
            Math.Max(2, (Height - 30) / 2), 32, 30);
    }

    private void ApplyTheme()
    {
        if (_combo is null || _searchButton is null) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Border;
        _frame.BackColor = theme.Surface;
        _combo.BackColor = theme.Surface;
        _combo.ForeColor = theme.Foreground;
        _combo.Invalidate();
        _searchButton.BackColor = theme.Surface;
        _searchButton.ForeColor = theme.Foreground;
        Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            if (!IsHandleCreated) return;
            try { BeginInvoke((Action)ApplyTheme); }
            catch (InvalidOperationException) { /* Handle is closing. */ }
        }
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
