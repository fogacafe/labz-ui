using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>A provider-backed lookup with optional tabular results. The consumer owns data access.</summary>
[DefaultEvent(nameof(SelectionChanged))]
public class LabzSearchLookup : UserControl
{
    private readonly TextBox _query = new() { BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Search..." };
    private readonly Button _search = new() { Text = "⌕", AccessibleName = "Search", TabStop = true };
    private readonly DataGridView _results = new()
    {
        ReadOnly = true, MultiSelect = false, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
        RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        Visible = false, TabStop = true
    };
    private readonly Panel _popupPanel = new() { Size = new Size(360, 220) };
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleLeft, Visible = false };
    private readonly ToolStripDropDown _popup = new() { AutoClose = true, Padding = Padding.Empty };
    private readonly System.Windows.Forms.Timer _timer = new();
    private CancellationTokenSource? _pending;
    private long _version;
    private bool _disposed;
    private IReadOnlyList<LabzLookupItem> _items = [];
    private IReadOnlyList<LabzLookupColumn> _columns = [];
    private LabzLookupItem? _selectedItem;
    private int _maxResults = 10;
    private bool _settingSelection;
    private int _debounceMilliseconds = 350;

    public LabzSearchLookup()
    {
        _query.AccessibleName = "Lookup search";
        _results.AccessibleName = "Lookup results";
        _timer.Interval = _debounceMilliseconds;
        _timer.Tick += (_, _) => { _timer.Stop(); _ = SearchOnEventAsync(); };
        _query.TextChanged += (_, _) =>
        {
            if (_settingSelection) return;
            _selectedItem = null;
            CancelPending();
            _timer.Stop();
            _popup.Close();
            if (SearchProvider is not null) _timer.Start();
        };
        _query.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = SearchOnEventAsync(); }
            if (e.KeyCode == Keys.Down && _popup.Visible && _results.Rows.Count > 0)
            {
                _results.Focus();
                if (_results.CurrentCell is null) _results.CurrentCell = _results.Rows[0].Cells[0];
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            if (e.KeyCode == Keys.Escape) _popup.Close();
        };
        _search.Click += (_, _) => _ = SearchOnEventAsync();
        _results.CellDoubleClick += (_, e) => SelectRow(e.RowIndex);
        _results.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                SelectRow(_results.CurrentCell?.RowIndex ?? -1);
            }
            if (e.KeyCode == Keys.Escape) { _popup.Close(); _query.Focus(); }
        };
        _results.Dock = DockStyle.Fill;
        _results.Visible = true;
        _popupPanel.Controls.Add(_results);
        _popupPanel.Controls.Add(_status);
        _popup.Items.Add(new ToolStripControlHost(_popupPanel)
        {
            AutoSize = false,
            Size = _popupPanel.Size,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        });
        Controls.AddRange([_query, _search]);
        Size = new Size(360, 36);
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
        try { await SearchAsync(); }
        catch (OperationCanceledException) { /* A newer search superseded this one. */ }
        catch (Exception ex)
        {
            if (!_disposed && !_query.IsDisposed && !IsSearching && _pending is null &&
                ex is not ObjectDisposedException)
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
        foreach (var item in _items)
        {
            var values = _columns.Count == 0
                ? new object?[] { item.DisplayText }
                : _columns.Select(column => item.Values.TryGetValue(column.Key, out var value) ? value : null).ToArray();
            _results.Rows.Add(values);
        }
        if (_items.Count == 0) ShowStatus("No results found.");
        else
        {
            _status.Visible = false;
            _results.Visible = true;
            OpenPopup();
        }
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
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LayoutChildren()
    {
        if (_query is null || _search is null || _results is null) return;
        _query.Bounds = new Rectangle(0, 0, Math.Max(1, Width - 40), 32);
        _search.Bounds = new Rectangle(Math.Max(0, Width - 38), 0, 38, 32);
        _popupPanel.Width = Math.Max(Width, 260);
        _popup.Items[0].Size = _popupPanel.Size;
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
        if (!_popup.Visible) _popup.Show(new Point(x, Math.Max(screen.Top, y)));
    }

    private void ApplyTheme()
    {
        if (_query is null || _search is null || _results is null) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        _query.BackColor = theme.Surface;
        _query.ForeColor = theme.Foreground;
        _search.BackColor = theme.Surface;
        _search.ForeColor = theme.Foreground;
        _popupPanel.BackColor = theme.Surface;
        _status.BackColor = theme.Surface;
        _status.ForeColor = theme.Foreground;
        _results.BackgroundColor = theme.Surface;
        _results.DefaultCellStyle.BackColor = theme.Surface;
        _results.DefaultCellStyle.ForeColor = theme.Foreground;
        _results.ColumnHeadersDefaultCellStyle.BackColor = theme.Surface;
        _results.ColumnHeadersDefaultCellStyle.ForeColor = theme.Foreground;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke((Action)ApplyTheme);
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
            _popup.Close();
            _popup.Dispose();
            CancelPending();
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
