using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using Labz.UI.Core.Theming;
using Labz.UI.Rendering.Drawing;

namespace Labz.UI.WinForms.Controls;

/// <summary>A culture-aware date editor with Labz-painted chrome and an anchored calendar.</summary>
[DefaultEvent(nameof(ValueChanged))]
public class LabzDatePicker : UserControl
{
    private readonly TextBox _editor = new() { BorderStyle = BorderStyle.None };
    private readonly Panel _calendar = new() { Size = new Size(280, 292) };
    private readonly Label _monthTitle = new() { TextAlign = ContentAlignment.MiddleCenter };
    private readonly Button _previous = new() { Text = "‹", FlatStyle = FlatStyle.Flat, TabStop = false };
    private readonly Button _next = new() { Text = "›", FlatStyle = FlatStyle.Flat, TabStop = false };
    private readonly Label[] _weekdays = new Label[7];
    private readonly Button[] _days = new Button[42];
    private DateTime _visibleMonth;
    private CalendarView _calendarView = CalendarView.Days;
    private int _yearPageStart;
    private enum CalendarView { Days, Months, Years }
    private readonly ToolStripDropDown _popup = new() { AutoClose = true, Padding = Padding.Empty, Margin = Padding.Empty };
    private readonly List<ScrollableControl> _scrollParents = [];
    private Form? _owner;
    private DateTime _value = DateTime.Now;
    private DateTime _minDate = DateTimePicker.MinimumDateTime;
    private DateTime _maxDate = DateTimePicker.MaximumDateTime;
    private bool _showTime;
    private bool _formatting;
    private bool _hovered;

    public LabzDatePicker()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Size = new Size(240, 44);
        TabStop = true;
        Controls.Add(_editor);
        _visibleMonth = new DateTime(_value.Year, _value.Month, 1);
        _previous.FlatAppearance.BorderSize = 0;
        _next.FlatAppearance.BorderSize = 0;
        _previous.Click += (_, _) => Navigate(-1);
        _next.Click += (_, _) => Navigate(1);
        _monthTitle.Cursor = Cursors.Hand;
        _monthTitle.Click += (_, _) =>
        {
            _calendarView = _calendarView == CalendarView.Days ? CalendarView.Months : CalendarView.Years;
            if (_calendarView == CalendarView.Years)
                _yearPageStart = Math.Clamp((_visibleMonth.Year - 1) / 12 * 12 + 1, 1, 9988);
            RenderCalendar();
        };
        _calendar.Controls.AddRange([_previous, _next, _monthTitle]);
        for (var i = 0; i < 7; i++)
        {
            _weekdays[i] = new Label { TextAlign = ContentAlignment.MiddleCenter };
            _calendar.Controls.Add(_weekdays[i]);
        }
        for (var i = 0; i < 42; i++)
        {
            var button = new Button { FlatStyle = FlatStyle.Flat, TabStop = false };
            button.FlatAppearance.BorderSize = 0;
            button.Click += OnDayClicked;
            _days[i] = button;
            _calendar.Controls.Add(button);
        }
        _editor.Enter += (_, _) => Invalidate();
        _editor.Leave += (_, _) => { if (!_popup.Visible) CommitText(); Invalidate(); };
        _editor.KeyDown += OnEditorKeyDown;
        _editor.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _editor.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        _editor.TextChanged += (_, _) => { if (!_formatting) OnTextChanged(EventArgs.Empty); };
        _popup.Items.Add(new ToolStripControlHost(_calendar)
        {
            AutoSize = false,
            Size = _calendar.Size,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        });
        _popup.Closed += (_, _) => { DetachOwner(); Invalidate(); };
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        FormatValue();
        LayoutEditor();
    }

    [Category("Action")]
    public event EventHandler? ValueChanged;

    [Category("Behavior")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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
            FormatValue();
            if (_popup.Visible) RenderCalendar();
        }
    }

    [Category("Behavior")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

    [Browsable(false)]
    public DateTimePickerFormat Format => DateTimePickerFormat.Custom;

    [Browsable(false)]
    public string CustomFormat => CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern +
        (ShowTime ? " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern : "");

    public override string Text
    {
        get => _editor?.Text ?? string.Empty;
        set { if (_editor is not null) _editor.Text = value ?? string.Empty; }
    }

    private void FormatValue()
    {
        if (_editor is null) return;
        _formatting = true;
        try { _editor.Text = Value.ToString(CustomFormat, CultureInfo.CurrentCulture); }
        finally { _formatting = false; }
    }

    private void CommitText()
    {
        if (DateTime.TryParse(_editor.Text, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
            Value = parsed;
        else FormatValue();
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
        {
            TogglePopup();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape && _popup.Visible)
        {
            _popup.Close();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            CommitText();
            _popup.Close();
            e.SuppressKeyPress = true;
        }
    }

    private void TogglePopup()
    {
        if (_popup.Visible) { _popup.Close(); return; }
        CommitText();
        _visibleMonth = new DateTime(Value.Year, Value.Month, 1);
        _calendarView = CalendarView.Days;
        _yearPageStart = Math.Clamp((Value.Year - 1) / 12 * 12 + 1, 1, 9988);
        RenderCalendar();
        AttachOwner();
        // Use control-relative placement, never absolute screen coordinates.
        _popup.Show(this, new Point(0, Height), ToolStripDropDownDirection.BelowRight);
        Invalidate();
    }

    private void Navigate(int delta)
    {
        if (_calendarView == CalendarView.Years)
        {
            _yearPageStart = Math.Clamp(_yearPageStart + delta * 12, 1, 9988);
        }
        else if (_calendarView == CalendarView.Months)
        {
            var year = Math.Clamp(_visibleMonth.Year + delta, 1, 9999);
            _visibleMonth = new DateTime(year, _visibleMonth.Month, 1);
        }
        else
        {
            if ((delta < 0 && _visibleMonth.Year == 1 && _visibleMonth.Month == 1) ||
                (delta > 0 && _visibleMonth.Year == 9999 && _visibleMonth.Month == 12)) return;
            _visibleMonth = _visibleMonth.AddMonths(delta);
        }
        RenderCalendar();
    }

    private void OnDayClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button) return;
        if (_calendarView == CalendarView.Years && button.Tag is int year)
        {
            _visibleMonth = new DateTime(year, _visibleMonth.Month, 1);
            _calendarView = CalendarView.Months;
            RenderCalendar();
            return;
        }
        if (_calendarView == CalendarView.Months && button.Tag is int month)
        {
            _visibleMonth = new DateTime(_visibleMonth.Year, month, 1);
            _calendarView = CalendarView.Days;
            RenderCalendar();
            return;
        }
        if (button.Tag is not DateTime date) return;
        Value = date.Date.Add(ShowTime ? Value.TimeOfDay : TimeSpan.Zero);
        _popup.Close();
        _editor.Focus();
    }

    private void RenderCalendar()
    {
        var theme = UiThemeManager.Current;
        _calendar.BackColor = theme.Surface;
        _popup.BackColor = theme.Surface;
        if (_calendarView == CalendarView.Years && _yearPageStart == 0)
            _yearPageStart = Math.Clamp((_visibleMonth.Year - 1) / 12 * 12 + 1, 1, 9988);
        _monthTitle.Text = _calendarView switch
        {
            CalendarView.Days => _visibleMonth.ToString("Y", CultureInfo.CurrentCulture),
            CalendarView.Months => _visibleMonth.Year.ToString(CultureInfo.CurrentCulture),
            _ => $"{_yearPageStart} – {Math.Min(9999, _yearPageStart + 11)}"
        };
        _monthTitle.ForeColor = theme.Foreground;
        _monthTitle.BackColor = theme.Surface;
        _monthTitle.Bounds = new Rectangle(38, 8, 204, 30);
        _previous.Bounds = new Rectangle(6, 8, 30, 30);
        _next.Bounds = new Rectangle(244, 8, 30, 30);
        foreach (var arrow in new[] { _previous, _next })
        {
            arrow.BackColor = theme.Surface;
            arrow.ForeColor = theme.Foreground;
        }
        if (_calendarView != CalendarView.Days)
        {
            foreach (var label in _weekdays) label.Visible = false;
            for (var i = 0; i < _days.Length; i++)
            {
                var button = _days[i];
                button.Visible = i < 12;
                if (i >= 12) continue;
                var isMonth = _calendarView == CalendarView.Months;
                var value = isMonth ? i + 1 : _yearPageStart + i;
                var selected = isMonth
                    ? _visibleMonth.Year == Value.Year && value == Value.Month
                    : value == Value.Year;
                button.Bounds = new Rectangle(8 + (i % 3) * 90, 58 + (i / 3) * 54, 84, 46);
                button.Tag = value;
                button.Text = isMonth
                    ? CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[i]
                    : value.ToString(CultureInfo.CurrentCulture);
                button.Enabled = isMonth
                    ? new DateTime(_visibleMonth.Year, value, 1) <= MaxDate.Date &&
                      new DateTime(_visibleMonth.Year, value, DateTime.DaysInMonth(_visibleMonth.Year, value)) >= MinDate.Date
                    : value >= MinDate.Year && value <= MaxDate.Year;
                button.BackColor = selected ? theme.Primary : theme.Surface;
                button.ForeColor = selected ? Color.White : theme.Foreground;
            }
            return;
        }
        foreach (var label in _weekdays) label.Visible = true;
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
        var firstCell = _visibleMonth.AddDays(-Math.Min(offset, (int)(_visibleMonth - DateTime.MinValue).TotalDays));
        for (var i = 0; i < _days.Length; i++)
        {
            var available = i <= (DateTime.MaxValue - firstCell).TotalDays;
            var day = available ? firstCell.AddDays(i) : DateTime.MaxValue;
            var button = _days[i];
            button.Visible = true;
            button.Bounds = new Rectangle(7 + (i % 7) * 38, 74 + (i / 7) * 34, 38, 32);
            button.Tag = day;
            button.Text = day.Day.ToString(CultureInfo.CurrentCulture);
            button.Enabled = available && day.Date >= MinDate.Date && day.Date <= MaxDate.Date;
            button.BackColor = day.Date == Value.Date ? theme.Primary : theme.Surface;
            button.ForeColor = day.Date == Value.Date ? Color.White :
                day.Month == _visibleMonth.Month ? theme.Foreground : theme.Border;
        }
    }

    private void AttachOwner()
    {
        DetachOwner();
        _owner = FindForm();
        if (_owner is not null)
        {
            _owner.Move += CloseForOwnerChange;
            _owner.Resize += CloseForOwnerChange;
            _owner.VisibleChanged += CloseForOwnerChange;
            _owner.FormClosed += CloseForOwnerClosed;
        }
        for (Control? parent = Parent; parent is not null; parent = parent.Parent)
        {
            parent.LocationChanged += CloseForOwnerChange;
            parent.VisibleChanged += CloseForOwnerChange;
            if (parent is ScrollableControl scroll)
            {
                scroll.Scroll += CloseForScroll;
                _scrollParents.Add(scroll);
            }
        }
    }

    private void DetachOwner()
    {
        if (_owner is not null)
        {
            _owner.Move -= CloseForOwnerChange;
            _owner.Resize -= CloseForOwnerChange;
            _owner.VisibleChanged -= CloseForOwnerChange;
            _owner.FormClosed -= CloseForOwnerClosed;
            _owner = null;
        }
        for (Control? parent = Parent; parent is not null; parent = parent.Parent)
        {
            parent.LocationChanged -= CloseForOwnerChange;
            parent.VisibleChanged -= CloseForOwnerChange;
        }
        foreach (var scroll in _scrollParents) scroll.Scroll -= CloseForScroll;
        _scrollParents.Clear();
    }

    private void CloseForOwnerChange(object? sender, EventArgs e) => _popup.Close();
    private void CloseForScroll(object? sender, ScrollEventArgs e) => _popup.Close();
    private void CloseForOwnerClosed(object? sender, FormClosedEventArgs e) => _popup.Close();

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (!Visible) _popup.Close();
    }
    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        if (_popup is not null && _popup.Visible) _popup.Close();
    }
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutEditor();
        if (_popup is not null && _popup.Visible) _popup.Close();
    }
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_editor is not null) { _editor.Font = Font; LayoutEditor(); }
    }
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

    private void LayoutEditor()
    {
        if (_editor is null) return;
        _editor.Bounds = new Rectangle(12, Math.Max(2, (Height - _editor.PreferredHeight) / 2),
            Math.Max(1, Width - 56), _editor.PreferredHeight);
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
            _popup.Close();
            DetachOwner();
            _popup.Dispose();
        }
        base.Dispose(disposing);
    }
}
