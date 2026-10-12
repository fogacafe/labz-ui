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
    private bool _hovered;
    private readonly ListBox _list = new() { BorderStyle = BorderStyle.None, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 28 };
    private LabzRoundedPopup? _roundedPopup;
    private readonly ToolStripDropDown _popup = new() { AutoClose = true, Padding = Padding.Empty };
    private readonly LabzPopupLifecycle _popupLifecycle;
    private readonly Label _selectionDisplay = new() { AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };

    public LabzComboBox()
    {
        _popupLifecycle = new LabzPopupLifecycle(this, _popup);
        _combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            IntegralHeight = false,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 25,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };
        _combo.DrawItem += DrawComboItem;
        _combo.Visible = false;
        _list.DrawItem += DrawListItem;
        _popup.BackColor = UiThemeManager.Current.Surface;
        _list.Click += (_, _) => CommitSelection();
        _list.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { CommitSelection(); e.Handled = true; } else if (e.KeyCode == Keys.Escape) { _popup.Close(); Focus(); e.Handled = true; } };
        _popup.Items.Add(new ToolStripControlHost(_list) { AutoSize = false, Margin = Padding.Empty, Padding = Padding.Empty });
        _roundedPopup = new LabzRoundedPopup(_popup, _list);
        _popup.Closed += (_, _) => Invalidate();
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
        _selectionDisplay.Click += (_, _) => ToggleDropDown();
        _selectionDisplay.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _selectionDisplay.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        _selectionDisplay.TabStop = false;
        TabStop = true;
        _combo.Enter += (_, _) => Invalidate();
        _combo.Leave += (_, _) => Invalidate();
        _combo.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _combo.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        Controls.Add(_combo);
        Controls.Add(_selectionDisplay);
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
        Invalidate();
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
        _combo.Bounds = new Rectangle(0, 0, 1, 1);
        _selectionDisplay.Bounds = new Rectangle(12, 4, Math.Max(1, Width - actionWidth - 44), Math.Max(1, Height - 8));
        _selectionDisplay.BringToFront();
        _searchButton.Bounds = new Rectangle(Math.Max(0, Width - 36),
            Math.Max(2, (Height - 30) / 2), 32, 30);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left) ToggleDropDown();
    }

    private void ToggleDropDown()
    {
        if (!Enabled) return;
        if (_popup.Visible) { _popup.Close(); return; }
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            foreach (var item in _combo.Items) _list.Items.Add(item);
            _list.SelectedIndex = _combo.SelectedIndex;
        }
        finally { _list.EndUpdate(); }
        var height = Math.Min(224, Math.Max(32, _list.Items.Count * _list.ItemHeight + 4));
        var size = new Size(Math.Max(Width, 140), height);
        _list.Size = size;
        _popup.Items[0].Size = size;
        _popup.Size = new Size(size.Width + 2, size.Height + 2);
        var screen = Screen.FromControl(this).WorkingArea;
        var anchor = PointToScreen(Point.Empty);
        var x = Math.Clamp(anchor.X, screen.Left, Math.Max(screen.Left, screen.Right - size.Width));
        var y = anchor.Y + Height + size.Height <= screen.Bottom ? anchor.Y + Height : Math.Max(screen.Top, anchor.Y - size.Height);
        _popupLifecycle.Attach();
        _popup.Show(new Point(x, y));
        _list.Focus();
        Invalidate();
    }

    private void CommitSelection()
    {
        if (_list.SelectedIndex >= 0 && _list.SelectedIndex < _combo.Items.Count)
            _combo.SelectedIndex = _list.SelectedIndex;
        _popup.Close();
        Focus();
        Invalidate();
    }

    private void DrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _list.Items.Count) return;
        var theme = UiThemeManager.Current;
        var selected = (e.State & DrawItemState.Selected) != 0;
        using var fill = new SolidBrush(selected ? theme.Primary : theme.Surface);
        e.Graphics.FillRectangle(fill, e.Bounds);
        TextRenderer.DrawText(e.Graphics, _combo.GetItemText(_list.Items[e.Index]), Font,
            new Rectangle(e.Bounds.X + 12, e.Bounds.Y, Math.Max(1, e.Bounds.Width - 16), e.Bounds.Height),
            selected ? Color.White : theme.Foreground, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Alt | Keys.Down) || keyData == Keys.F4 || keyData == Keys.Space)
        {
            ToggleDropDown();
            return true;
        }
        if (keyData == Keys.Down || keyData == Keys.Up)
        {
            if (_combo.Items.Count == 0) return true;
            SelectedIndex = Math.Clamp(SelectedIndex + (keyData == Keys.Down ? 1 : -1), 0, _combo.Items.Count - 1);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void UpdateSelectionDisplay()
    {
        if (_combo is null || _selectionDisplay is null) return;
        _selectionDisplay.Visible = true;
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
        using var border = new Pen(Focused || _popup.Visible ? theme.Primary : _hovered ? ControlPaint.Dark(theme.Border) : theme.Border, Focused || _popup.Visible ? 2 : 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        var actionWidth = _showSearchButton ? 36 : 0;
        var arrowCenterX = Width - actionWidth - 20;
        var arrowCenterY = Height / 2;
        using var arrowPen = new Pen(Enabled ? theme.Foreground : theme.Border, 1.6f)
        { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawLines(arrowPen, [
            new Point(arrowCenterX - 4, arrowCenterY - 2),
            new Point(arrowCenterX, arrowCenterY + 2),
            new Point(arrowCenterX + 4, arrowCenterY - 2)]);
    }

    private void ApplyTheme()
    {
        if (_combo is null || _searchButton is null) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        _selectionDisplay.ForeColor = theme.Foreground;
        _selectionDisplay.BackColor = theme.Surface;
        _combo.BackColor = theme.Surface;
        _combo.ForeColor = theme.Foreground;
        _combo.Invalidate();
        _popup.BackColor = theme.Surface;
        _list.BackColor = theme.Surface;
        _list.ForeColor = theme.Foreground;
        _list.Invalidate();
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

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) _popup.Close();
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (_popup is null) return;
        _popup.Close();
        // Owner and ancestor handlers attach on the next popup opening.
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _popupLifecycle.Dispose();
            UiThemeManager.ThemeChanged -= OnThemeChanged;
            _popup.Close();
            _roundedPopup?.Dispose();
            _popup.Dispose();
        }
        base.Dispose(disposing);
    }
}
