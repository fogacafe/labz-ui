using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware date editor with a custom calendar popup.</summary>
[DefaultEvent(nameof(ValueChanged))]
public class LabzDatePicker : UserControl
{
    private readonly TextBox _editor = new() { BorderStyle = BorderStyle.None };
    private readonly ToolStripDropDown _popup = new() { AutoClose = true, Padding = Padding.Empty };
    private readonly Panel _calendar = new() { Size = new Size(280, 290) };
    private readonly Label _monthLabel = new() { TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _previous = new() { Text = "‹", FlatStyle = FlatStyle.Flat, TabStop = false };
    private readonly Button _next = new() { Text = "›", FlatStyle = FlatStyle.Flat, TabStop = false };
    private readonly Button[] _days = new Button[42];
    private readonly Label[] _weekdays = new Label[7];
    private DateTime _value = DateTime.Now;
    private DateTime _minDate = DateTimePicker.MinimumDateTime;
    private DateTime _maxDate = DateTimePicker.MaximumDateTime;
    private DateTime _visibleMonth;
    private bool _showTime;
    private bool _hovered;
    private bool _formatting;
    private Form? _owner;

    public LabzDatePicker()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(240, 42);
        TabStop = true;
        _visibleMonth = new DateTime(_value.Year, _value.Month, 1);
        Controls.Add(_editor);
        _editor.TextChanged += (_, _) => { if (!_formatting) OnTextChanged(EventArgs.Empty); };
        _editor.Enter += (_, _) => Invalidate();
        _editor.Leave += (_, _) => { if (!_popup.Visible) CommitText(); Invalidate(); };
        _editor.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
            { TogglePopup(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Escape && _popup.Visible)
            { _popup.Close(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Enter)
            { CommitText(); _popup.Close(); e.SuppressKeyPress = true; }
        };
        _editor.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _editor.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        _previous.FlatAppearance.BorderSize = 0;
        _next.FlatAppearance.BorderSize = 0;
        _previous.Click += (_, _) => ChangeMonth(-1);
        _next.Click += (_, _) => ChangeMonth(1);
        _calendar.Controls.AddRange([_previous, _next, _monthLabel]);
        for (var i = 0; i < 7; i++)
        {
            _weekdays[i] = new Label { TextAlign = ContentAlignment.MiddleCenter };
            _calendar.Controls.Add(_weekdays[i]);
        }
        for (var i = 0; i < 42; i++)
        {
            var dayButton = new Button { FlatStyle = FlatStyle.Flat, TabStop = false, Tag = i };
            dayButton.FlatAppearance.BorderSize = 0;
            dayButton.Click += OnDayClicked;
            _days[i] = dayButton;
            _calendar.Controls.Add(dayButton);
        }
        _popup.Items.Add(new ToolStripControlHost(_calendar)
        { AutoSize = false, Size = _calendar.Size, Padding = Padding.Empty, Margin = Padding.Empty });
        _popup.Closed += (_, _) => { _editor.Focus(); Invalidate(); };
        UiThemeManager.ThemeChanged += OnThemeChanged;
        LayoutEditor();
        ApplyTheme();
        FormatValue();
    }

    [Category("Action")]
    public event EventHandler? ValueChanged;

    [Category("Behavior")]
    public DateTime Value
    {
        get => _value;
        set
        {
            var next = value < MinDate ? MinDate : value > MaxDate ? MaxDate : value;
            if (_value != next)
            {
                _value = next;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
            _visibleMonth = new DateTime(_value.Year, _value.Month, 1);
            FormatValue();
            if (_popup.Visible) RenderCalendar();
        }
    }

    [Category("Behavior")]
    public DateTime MinDate
    {
        get => _minDate;
        set
        {
            if (value > _maxDate) throw new ArgumentOutOfRangeException(nameof(value));
            _minDate = value;
            Value = _value;
        }
    }

    [Category("Behavior")]
    public DateTime MaxDate
    {
        get => _maxDate;
        set
        {
            if (value < _minDate) throw new ArgumentOutOfRangeException(nameof(value));
            _maxDate = value;
            Value = _value;
        }
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool ShowTime
    {
        get => _showTime;
        set { _showTime = value; FormatValue(); }
    }

    // Keep these readable for existing callers; the composed editor always uses a localized custom pattern.
    [Browsable(false)]
    public DateTimePickerFormat Format => DateTimePickerFormat.Custom;

    [Browsable(false)]
    public string CustomFormat => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern +
        (ShowTime ? " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern : "");

    [Browsable(true)]
    public override string Text
    {
        get => _editor?.Text ?? string.Empty;
        set { if (_editor is not null) _editor.Text = value ?? string.Empty; }
    }

    private void FormatValue()
    {
        if (_editor is null) return;
        _formatting = true;
        try { _editor.Text = _value.ToString(CustomFormat, CultureInfo.CurrentCulture); }
        finally { _formatting = false; }
    }

    private void CommitText()
    {
        if (DateTime.TryParse(_editor.Text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
            Value = parsed;
        else FormatValue();
    }

    private void LayoutEditor()
    {
        if (_editor is null) return;
        _editor.Bounds = new Rectangle(12, Math.Max(2, (Height - _editor.PreferredHeight) / 2),
            Math.Max(1, Width - 56), _editor.PreferredHeight);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_editor is not null) { _editor.Font = Font; LayoutEditor(); }
    }
    protected override void OnSizeChanged(EventArgs e) { base.OnSizeChanged(e); LayoutEditor(); }
    protected override void OnEnter(EventArgs e) { base.OnEnter(e); _editor.Focus(); }
    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        if (_editor is not null) _editor.Enabled = Enabled;
        Invalidate();
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;
        if (e.X >= Width - 42) TogglePopup();
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
        using var pen = new Pen(_editor.Focused || _popup.Visible ? theme.Primary :
            _hovered ? ControlPaint.Dark(theme.Border) : theme.Border,
            _editor.Focused || _popup.Visible ? 2 : 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(pen, path);
        var x = Width - 24;
        var y = Height / 2;
        using var glyph = new Pen(Enabled ? theme.Foreground : theme.Border, 1.5f);
        e.Graphics.DrawRectangle(glyph, x - 6, y - 5, 12, 11);
        e.Graphics.DrawLine(glyph, x - 6, y - 2, x + 6, y - 2);
        e.Graphics.DrawLine(glyph, x - 3, y - 8, x - 3, y - 4);
        e.Graphics.DrawLine(glyph, x + 3, y - 8, x + 3, y - 4);
    }

    private void TogglePopup()
    {
        if (_popup.Visible) { _popup.Close(); return; }
        CommitText();
        _visibleMonth = new DateTime(Value.Year, Value.Month, 1);
        RenderCalendar();
        AttachOwner();
        var screen = Screen.FromControl(this).WorkingArea;
        var origin = PointToScreen(Point.Empty);
        var x = Math.Clamp(origin.X, screen.Left, Math.Max(screen.Left, screen.Right - _calendar.Width));
        var y = origin.Y + Height + _calendar.Height <= screen.Bottom
            ? origin.Y + Height : Math.Max(screen.Top, origin.Y - _calendar.Height);
        _popup.Show(new Point(x, y));
        Invalidate();
    }

    private void ChangeMonth(int delta)
    {
        var next = _visibleMonth.AddMonths(delta);
        if (next.Year < MinDate.Year || next.Year > MaxDate.Year) return;
        _visibleMonth = next;
        RenderCalendar();
    }

    private void OnDayClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { Tag: DateTime date }) return;
        var chosen = date.Date.Add(ShowTime ? Value.TimeOfDay : TimeSpan.Zero);
        Value = chosen;
        _popup.Close();
    }

    private void RenderCalendar()
    {
        var theme = UiThemeManager.Current;
        _calendar.BackColor = theme.Surface;
        _popup.BackColor = theme.Surface;
        _monthLabel.Text = _visibleMonth.ToString("Y", CultureInfo.CurrentCulture);
        _monthLabel.ForeColor = theme.Foreground;
        _monthLabel.BackColor = theme.Surface;
        _monthLabel.Bounds = new Rectangle(38, 8, 204, 30);
        _previous.Bounds = new Rectangle(6, 8, 30, 30);
        _next.Bounds = new Rectangle(244, 8, 30, 30);
        foreach (var arrow in new[] { _previous, _next })
        {
            arrow.BackColor = theme.Surface;
            arrow.ForeColor = theme.Foreground;
        }
        var firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        for (var col = 0; col < 7; col++)
        {
            var weekday = (DayOfWeek)(((int)firstDay + col) % 7);
            var label = _weekdays[col];
            label.Text = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(int)weekday];
            label.ForeColor = theme.Foreground;
            label.BackColor = theme.Surface;
            label.Bounds = new Rectangle(7 + col * 38, 44, 38, 26);
        }
        var offset = ((int)_visibleMonth.DayOfWeek - (int)firstDay + 7) % 7;
        var firstCell = _visibleMonth.AddDays(-offset);
        for (var i = 0; i < _days.Length; i++)
        {
            var day = firstCell.AddDays(i);
            var button = _days[i];
            button.Bounds = new Rectangle(7 + (i % 7) * 38, 74 + (i / 7) * 34, 38, 32);
            button.Tag = day;
            button.Text = day.Day.ToString(CultureInfo.CurrentCulture);
            button.Enabled = day.Date >= MinDate.Date && day.Date <= MaxDate.Date;
            button.ForeColor = day.Month == _visibleMonth.Month ? theme.Foreground : theme.Border;
            button.BackColor = day.Date == Value.Date ? theme.Primary : theme.Surface;
            if (day.Date == Value.Date) button.ForeColor = Color.White;
        }
    }

    private void AttachOwner()
    {
        var form = FindForm();
        if (ReferenceEquals(form, _owner)) return;
        DetachOwner();
        _owner = form;
        if (form is null) return;
        form.Resize += OnOwnerResize;
        form.VisibleChanged += OnOwnerVisibleChanged;
        form.FormClosed += OnOwnerClosed;
    }
    private void OnOwnerResize(object? sender, EventArgs e)
    {
        if (_owner?.WindowState == FormWindowState.Minimized) _popup.Close();
    }
    private void OnOwnerVisibleChanged(object? sender, EventArgs e)
    {
        if (_owner?.Visible != true) _popup.Close();
    }
    private void OnOwnerClosed(object? sender, FormClosedEventArgs e) => _popup.Close();
    private void DetachOwner()
    {
        if (_owner is null) return;
        _owner.Resize -= OnOwnerResize;
        _owner.VisibleChanged -= OnOwnerVisibleChanged;
        _owner.FormClosed -= OnOwnerClosed;
        _owner = null;
    }
    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) _popup.Close();
    }
    private void ApplyTheme()
    {
        if (_editor is null) return;
        var theme = UiThemeManager.Current;
        _editor.BackColor = theme.Surface;
        _editor.ForeColor = theme.Foreground;
        if (_popup.Visible) RenderCalendar();
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
        if (disposing)
        {
            UiThemeManager.ThemeChanged -= OnThemeChanged;
            DetachOwner();
            _popup.Close();
            _popup.Dispose();
        }
        base.Dispose(disposing);
    }
}
