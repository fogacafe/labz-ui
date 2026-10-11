using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Rendering.Drawing;
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
    private readonly Button _arrow = new() { Text = "▾", FlatStyle = FlatStyle.Flat, TabStop = false, Cursor = Cursors.Hand };
    private bool _hovered;
    private readonly Label _selectionDisplay = new() { AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand, BackColor = Color.Transparent };

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
        _combo.SelectedIndexChanged += (_, e) => { UpdateSelectionDisplay(); SelectedIndexChanged?.Invoke(this, e); };
        _combo.TextChanged += (_, _) => { UpdateSelectionDisplay(); OnTextChanged(EventArgs.Empty); };
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
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _combo.FlatStyle = FlatStyle.Flat;
        _arrow.FlatAppearance.BorderSize = 0;
        _arrow.Click += (_, _) => ToggleDropDown();
        _selectionDisplay.Click += (_, _) => ToggleDropDown();
        _selectionDisplay.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _selectionDisplay.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        _combo.Enter += (_, _) => Invalidate();
        _combo.Leave += (_, _) => Invalidate();
        _combo.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _combo.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        Controls.Add(_combo);
        Controls.Add(_selectionDisplay);
        Controls.Add(_arrow);
        Controls.Add(_searchButton);
        Size = new Size(240, 42);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        LayoutChildren();
        UpdateSelectionDisplay();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("Data")]
    public ComboBox.ObjectCollection Items => _combo.Items;

    [Category("Data")]
    [DefaultValue(null)]
    public object? DataSource
    {
        get => _combo.DataSource;
        set { _combo.DataSource = value; UpdateSelectionDisplay(); }
    }

    [Category("Data")]
    [DefaultValue("")]
    public string DisplayMember
    {
        get => _combo.DisplayMember;
        set { _combo.DisplayMember = value; UpdateSelectionDisplay(); }
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
        set { _combo.DropDownStyle = value; UpdateSelectionDisplay(); }
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
        _combo.Bounds = new Rectangle(10, Math.Max(2, (Height - _combo.Height) / 2),
            Math.Max(1, Width - actionWidth - 18), _combo.Height);
        _selectionDisplay.Bounds = new Rectangle(12, 4, Math.Max(1, Width - actionWidth - 48), Math.Max(1, Height - 8));
        _selectionDisplay.BringToFront();
        _arrow.Bounds = new Rectangle(Math.Max(0, Width - actionWidth - 32), 4, 28, Math.Max(1, Height - 8));
        _searchButton.Bounds = new Rectangle(Math.Max(0, Width - 36),
            Math.Max(2, (Height - 30) / 2), 32, 30);
    }

    private void ToggleDropDown()
    {
        if (!Enabled) return;
        _combo.Focus();
        _combo.DroppedDown = !_combo.DroppedDown;
        Invalidate();
    }

    private void UpdateSelectionDisplay()
    {
        if (_combo is null || _selectionDisplay is null) return;
        _selectionDisplay.Visible = _combo.DropDownStyle == ComboBoxStyle.DropDownList;
        _selectionDisplay.Text = _combo.GetItemText(_combo.SelectedItem);
        _selectionDisplay.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        using var path = RoundedRectangles.Create(new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3)), theme.CornerRadius);
        using var fill = new SolidBrush(theme.Surface);
        using var border = new Pen(_combo.Focused ? theme.Primary : _hovered ? ControlPaint.Dark(theme.Border) : theme.Border, _combo.Focused ? 2 : 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
    }

    private void ApplyTheme()
    {
        if (_combo is null || _searchButton is null) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        _arrow.BackColor = theme.Surface;
        _arrow.ForeColor = theme.Foreground;
        _selectionDisplay.ForeColor = theme.Foreground;
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
