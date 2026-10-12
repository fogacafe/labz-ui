using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Rendering.Drawing;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>A provider-backed lookup with optional tabular results. The consumer owns data access.</summary>
[DefaultEvent(nameof(SelectionChanged))]
public class LabzSearchLookup : UserControl
{
    // DataGridView consumes Escape internally when a row/cell is current.
    // Handle it before the grid's default key processing.
    private sealed class LookupResultsGrid : DataGridView
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action? EscapeRequested { get; set; }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                EscapeRequested?.Invoke();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private readonly TextBox _query = new() { BorderStyle = BorderStyle.None, PlaceholderText = "Search..." };
    private readonly LookupResultsGrid _results = new()
    {
        ReadOnly = true, MultiSelect = false, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        Visible = false, TabStop = true
    };
    private readonly Panel _popupPanel = new() { Size = new Size(360, 220) };
    private bool _hovered;
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Visible = false };
    private readonly ToolStripDropDown _popup = new() { AutoClose = true, Padding = Padding.Empty };
    private readonly LabzPopupLifecycle _popupLifecycle;
    private readonly System.Windows.Forms.Timer _timer = new();
    private CancellationTokenSource? _pending;
    private long _version;
    private bool _disposed;
    private IReadOnlyList<LabzLookupItem> _items = [];
    private IReadOnlyList<LabzLookupColumn> _columns = [];
    private LabzLookupItem? _selectedItem;
    private int _maxResults = 10;
    private bool _settingSelection;
    private bool _highlightFirstOnResults;
    private bool _openingPopup;
    private readonly System.Windows.Forms.Timer _outsideClickMonitor = new() { Interval = 75 };

    private int _debounceMilliseconds = 350;

    public LabzSearchLookup()
    {
        _popupLifecycle = new LabzPopupLifecycle(this, _popup);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        _query.AccessibleName = "Lookup search";
        _query.Enter += (_, _) => Invalidate();
        _query.Leave += (_, _) => Invalidate();
        _query.MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
        _query.MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
        _results.AccessibleName = "Lookup results";
        _results.EscapeRequested = () => { _popup.Close(); _query.Focus(); };
        _timer.Interval = _debounceMilliseconds;
        _outsideClickMonitor.Tick += (_, _) => CheckPopupDismissal();
        _timer.Tick += (_, _) => { _timer.Stop(); _ = SearchOnEventAsync(); };
        _query.TextChanged += (_, _) =>
        {
            if (_settingSelection) return;
            _selectedItem = null;
            _highlightFirstOnResults = false;
            CancelPending();
            _timer.Stop();
            _popup.Close();
            if (SearchProvider is not null) _timer.Start();
        };
        _query.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = SearchOnEventAsync(); }
            if (e.KeyCode == Keys.Down)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (!_popup.Visible)
                {
                    if (_items.Count > 0) OpenPopup();
                    else if (SearchProvider is not null) _ = SearchOnEventAsync();
                }
                if (_popup.Visible && _results.Visible && _results.Rows.Count > 0)
                    FocusFirstResult();
            }
            if (e.KeyCode == Keys.Escape) _popup.Close();
        };
        _results.CellDoubleClick += (_, e) => SelectRow(e.RowIndex);
        _results.CellClick += (_, e) => { if (e.RowIndex >= 0) SelectRow(e.RowIndex); };
        _results.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SelectRow(_results.CurrentCell?.RowIndex ?? -1);
            }
            if (e.KeyCode == Keys.Escape) { _popup.Close(); _query.Focus(); e.Handled = true; e.SuppressKeyPress = true; }
        };
        _results.KeyPress += (_, e) =>
        {
            if (char.IsControl(e.KeyChar)) return;
            _query.Focus();
            _query.SelectionStart = _query.TextLength;
            _query.SelectionLength = 0;
            _query.SelectedText = e.KeyChar.ToString();
            e.Handled = true;
        };
        _results.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Back) return;
            _query.Focus();
            if (_query.SelectionLength == 0 && _query.SelectionStart > 0)
            {
                _query.SelectionStart--;
                _query.SelectionLength = 1;
            }
            _query.SelectedText = string.Empty;
            e.Handled = true;
            e.SuppressKeyPress = true;
        };
        _results.Dock = DockStyle.Fill;
        _results.RowHeadersVisible = false;
        _results.AllowUserToResizeColumns = false;
        _results.Visible = true;
        _popupPanel.Controls.Add(_results);
        _popupPanel.Controls.Add(_status);
        _popup.Closed += (_, _) => { _outsideClickMonitor.Stop(); Invalidate(); };
        _popup.Items.Add(new ToolStripControlHost(_popupPanel)
        {
            AutoSize = false,
            Size = _popupPanel.Size,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        });
        Controls.Add(_query);
        Size = new Size(360, 42);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        LayoutChildren();
    }

    /// <summary>Async provider called with query text, maximum count and cancellation token.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, int, CancellationToken, Task<IReadOnlyList<LabzLookupItem>>>? SearchProvider { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<LabzLookupColumn> Columns
    {
        get => _columns;
        set { _columns = value ?? []; BuildColumns(); }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LabzLookupItem? SelectedItem => _selectedItem;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<LabzLookupItem> Results => _items;

    [Category("Behavior")]
    [DefaultValue(10)]
    public int MaxResults
    {
        get => _maxResults;
        set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); _maxResults = value; }
    }

    [Category("Behavior")]
    [DefaultValue(350)]
    public int DebounceMilliseconds
    {
        get => _debounceMilliseconds;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _debounceMilliseconds = value;
            _timer.Interval = value;
        }
    }

    [Category("Appearance")]
    [DefaultValue("Search...")]
    public string PlaceholderText
    {
        get => _query.PlaceholderText;
        set => _query.PlaceholderText = value;
    }

    public override string Text
    {
        get => _query?.Text ?? string.Empty;
        set { if (_query is not null) _query.Text = value; }
    }

    public event EventHandler? SelectionChanged;

    [Browsable(false)]
    public bool IsPopupOpen => _popup.Visible;

    [Browsable(false)]
    public bool IsSearching { get; private set; }
    public event EventHandler<LabzLookupSearchFailedEventArgs>? SearchFailed;

    public async Task SearchAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer.Stop();
        CancelPending();
        var provider = SearchProvider;
        if (provider is null) return;
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _pending = source;
        var version = ++_version;
        var query = _query.Text;
        IsSearching = true;
        ShowStatus("Searching...");
        try
        {
            var items = await provider(query, MaxResults, source.Token);
            source.Token.ThrowIfCancellationRequested();
            if (_disposed || version != _version) return;
            _items = (items ?? []).Take(MaxResults).ToArray();
            RenderResults();
        }
        finally
        {
            if (ReferenceEquals(_pending, source))
            {
                _pending = null;
                IsSearching = false;
            }
        }
    }

    private async Task SearchOnEventAsync()
    {
        var expectedVersion = _version + 2;
        try { await SearchAsync(); }
        catch (OperationCanceledException) { /* A newer search superseded this one. */ }
        catch (Exception ex)
        {
            if (!_disposed && !_query.IsDisposed && expectedVersion == _version)
            {
                ShowStatus("Search failed. Try again.");
                SearchFailed?.Invoke(this, new LabzLookupSearchFailedEventArgs(ex));
            }
        }
    }

    private void CancelPending()
    {
        ++_version;
        _pending?.Cancel();
        IsSearching = false;
    }

    private void BuildColumns()
    {
        if (_results is null) return;
        _results.Columns.Clear();
        _results.ColumnHeadersVisible = _columns.Count > 0;
        if (_columns.Count == 0)
        {
            _results.Columns.Add("Display", "Result");
            return;
        }
        foreach (var column in _columns)
            _results.Columns.Add(column.Key, column.Header);
    }

    private void RenderResults()
    {
        BuildColumns();
        _results.Rows.Clear();
        _results.ClearSelection();
        foreach (var item in _items)
        {
            var values = _columns.Count == 0
                ? new object?[] { item.DisplayText }
                : _columns.Select(column => item.Values.TryGetValue(column.Key, out var value) ? value : null).ToArray();
            _results.Rows.Add(values);
        }
        _results.ClearSelection();
        if (_items.Count == 0) ShowStatus("No results found.");
        else
        {
            _status.Visible = false;
            _results.Visible = true;
            if (_highlightFirstOnResults && _results.Rows.Count > 0 && _results.Columns.Count > 0)
            {
                _results.CurrentCell = _results.Rows[0].Cells[0];
                _results.Rows[0].Selected = true;
            }
            _highlightFirstOnResults = false;
            OpenPopup();
        }
    }

    private void FocusFirstResult()
    {
        if (_results.Rows.Count == 0 || _results.Columns.Count == 0) return;
        var first = _results.Rows.Cast<DataGridViewRow>().FirstOrDefault(row => row.Visible);
        if (first is null) return;
        _results.Focus();
        _results.CurrentCell = first.Cells[0];
        first.Selected = true;
    }

    private void SelectRow(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        _selectedItem = _items[index];
        _timer.Stop();
        CancelPending();
        _settingSelection = true;
        try { _query.Text = _selectedItem.DisplayText; }
        finally { _settingSelection = false; }
        _timer.Stop();
        _popup.Close();
        _query.Focus();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LayoutChildren()
    {
        if (_query is null || _results is null) return;
        _query.Bounds = new Rectangle(12, Math.Max(3, (Height - _query.PreferredHeight) / 2), Math.Max(1, Width - 60), _query.PreferredHeight);
        _popupPanel.Width = Math.Max(1, Width);
        _popup.Items[0].Size = _popupPanel.Size;
        _popup.Size = _popupPanel.Size;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutChildren();
    }

    private void ShowStatus(string message)
    {
        if (_disposed || IsDisposed) return;
        _status.Text = message;
        _status.Visible = true;
        _results.Visible = false;
        OpenPopup();
    }

    private void OpenPopup()
    {
        if (_disposed || !IsHandleCreated) return;
        var screen = Screen.FromControl(this).WorkingArea;
        var anchor = PointToScreen(Point.Empty);
        var popupHeight = _popupPanel.Height;
        var popupWidth = Math.Min(_popupPanel.Width, screen.Width);
        var x = Math.Clamp(anchor.X, screen.Left, Math.Max(screen.Left, screen.Right - popupWidth));
        var below = screen.Bottom - (anchor.Y + Height);
        var above = anchor.Y - screen.Top;
        var y = below >= popupHeight || below >= above
            ? Math.Min(anchor.Y + Height, screen.Bottom - popupHeight)
            : Math.Max(screen.Top, anchor.Y - popupHeight);
        if (!_popup.Visible)
        {
            var keepTyping = _query.Focused;
            // A non-activating dropdown keeps the editor focused during async searches.
            // Dismissal is managed explicitly so clicks outside still close the popup.
            _popup.AutoClose = false;
            _popupLifecycle.Attach();
            _openingPopup = true;
            try
            {
                _popup.Show(new Point(x, Math.Max(screen.Top, y)));
                if (keepTyping) _query.Focus();
                _outsideClickMonitor.Start();
            }
            finally { _openingPopup = false; }
        }
    }

    private void CheckPopupDismissal()
    {
        if (_openingPopup || !_popup.Visible) { if (!_popup.Visible) _outsideClickMonitor.Stop(); return; }
        if (!Visible || FindForm() is not { Visible: true } owner || owner.WindowState == FormWindowState.Minimized)
        {
            _popup.Close();
            return;
        }
        // Mouse capture reports a click even when focus stays in the editor.
        // Only close for a press that starts outside both the input and results.
        if (Control.MouseButtons == MouseButtons.None) return;
        var cursor = Cursor.Position;
        if (!RectangleToScreen(ClientRectangle).Contains(cursor) && !_popup.Bounds.Contains(cursor))
            _popup.Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var theme = UiThemeManager.Current;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? theme.Background);
        using var path = RoundedRectangles.Create(new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3)), theme.CornerRadius);
        using var fill = new SolidBrush(theme.Surface);
        using var border = new Pen(_query.Focused ? theme.Primary : _hovered ? ControlPaint.Dark(theme.Border) : theme.Border, _query.Focused ? 2 : 1);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);
        var centerX = Width - 23;
        var centerY = Height / 2 - 2;
        using var glyph = new Pen(Enabled ? theme.Foreground : theme.Border, 1.6f)
        { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawEllipse(glyph, centerX - 5, centerY - 5, 9, 9);
        e.Graphics.DrawLine(glyph, centerX + 2, centerY + 2, centerX + 7, centerY + 7);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (Enabled && e.Button == MouseButtons.Left && e.X >= Width - 48)
        {
            _query.Focus();
            _highlightFirstOnResults = true;
            _ = SearchOnEventAsync();
        }
        else if (Enabled && e.Button == MouseButtons.Left)
            _query.Focus();
    }

    private void ApplyTheme()
    {
        if (_query is null || _results is null) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        _query.BackColor = theme.Surface;
        _query.ForeColor = theme.Foreground;
        _popup.BackColor = theme.Surface;
        _popupPanel.BackColor = theme.Surface;
        _status.BackColor = theme.Surface;
        _status.ForeColor = theme.Foreground;
        _results.BackgroundColor = theme.Surface;
        _results.BorderStyle = BorderStyle.None;
        _results.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _results.EnableHeadersVisualStyles = false;
        _results.RowTemplate.Height = 28;
        _results.GridColor = theme.Border;
        _results.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _results.ColumnHeadersHeight = 30;
        _results.DefaultCellStyle.BackColor = theme.Surface;
        _results.DefaultCellStyle.ForeColor = theme.Foreground;
        _results.DefaultCellStyle.SelectionBackColor = theme.Primary;
        _results.DefaultCellStyle.SelectionForeColor = Color.White;
        _results.RowsDefaultCellStyle.SelectionBackColor = theme.Primary;
        _results.RowsDefaultCellStyle.SelectionForeColor = Color.White;
        _results.AlternatingRowsDefaultCellStyle.BackColor = theme.Surface;
        _results.AlternatingRowsDefaultCellStyle.ForeColor = theme.Foreground;
        _results.AlternatingRowsDefaultCellStyle.SelectionBackColor = theme.Primary;
        _results.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        _results.ColumnHeadersDefaultCellStyle.BackColor = theme.Background;
        _results.ColumnHeadersDefaultCellStyle.ForeColor = theme.Foreground;
        _results.ColumnHeadersDefaultCellStyle.Font = Font;
        _results.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _results.DefaultCellStyle.Padding = new Padding(8, 0, 4, 0);
        _results.AlternatingRowsDefaultCellStyle.SelectionBackColor = theme.Primary;
        _results.Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_disposed || IsDisposed || Disposing) return;
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
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
            _outsideClickMonitor.Stop();
            _outsideClickMonitor.Dispose();
            _popup.Close();
            _popupLifecycle.Dispose();
            _popup.Dispose();
            CancelPending();
            _popupLifecycle.Dispose();
            UiThemeManager.ThemeChanged -= OnThemeChanged;
        }
        base.Dispose(disposing);
    }
}

/// <param name="Key">Stable identity supplied by the consumer.</param>
/// <param name="DisplayText">Text displayed when selected.</param>
/// <param name="Values">Optional values keyed by column identifiers.</param>
public sealed record LabzLookupItem(object Key, string DisplayText, IReadOnlyDictionary<string, object?> Values);

public sealed record LabzLookupColumn(string Key, string Header);

public sealed class LabzLookupSearchFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}
