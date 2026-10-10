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
    private readonly System.Windows.Forms.Timer _timer = new();
    private CancellationTokenSource? _pending;
    private long _version;
    private bool _disposed;
    private IReadOnlyList<LabzLookupItem> _items = [];
    private IReadOnlyList<LabzLookupColumn> _columns = [];
    private LabzLookupItem? _selectedItem;
    private int _maxResults = 10;
    private int _debounceMilliseconds = 350;

    public LabzSearchLookup()
    {
        _timer.Interval = _debounceMilliseconds;
        _timer.Tick += (_, _) => { _timer.Stop(); _ = SearchOnEventAsync(); };
        _query.TextChanged += (_, _) =>
        {
            CancelPending();
            _timer.Stop();
            if (SearchProvider is not null) _timer.Start();
        };
        _query.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = SearchOnEventAsync(); }
            if (e.KeyCode == Keys.Down && _results.Visible && _results.Rows.Count > 0)
            {
                _results.Focus();
                e.Handled = true;
            }
            if (e.KeyCode == Keys.Escape) _results.Visible = false;
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
            if (e.KeyCode == Keys.Escape) { _results.Visible = false; _query.Focus(); }
        };
        Controls.AddRange([_query, _search, _results]);
        Size = new Size(360, 240);
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
            if (ReferenceEquals(_pending, source)) _pending = null;
        }
    }

    private async Task SearchOnEventAsync()
    {
        try { await SearchAsync(); }
        catch (OperationCanceledException) { /* A newer search superseded this one. */ }
        catch (Exception ex)
        {
            if (!_disposed && !_query.IsDisposed)
                SearchFailed?.Invoke(this, new LabzLookupSearchFailedEventArgs(ex));
        }
    }

    private void CancelPending()
    {
        ++_version;
        _pending?.Cancel();
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
        _results.Visible = _items.Count > 0;
        _results.BringToFront();
    }

    private void SelectRow(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        _selectedItem = _items[index];
        _timer.Stop();
        CancelPending();
        _query.Text = _selectedItem.DisplayText;
        _timer.Stop();
        _results.Visible = false;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LayoutChildren()
    {
        if (_query is null || _search is null || _results is null) return;
        _query.Bounds = new Rectangle(0, 0, Math.Max(1, Width - 40), 32);
        _search.Bounds = new Rectangle(Math.Max(0, Width - 38), 0, 38, 32);
        _results.Bounds = new Rectangle(0, 38, Math.Max(1, Width), Math.Max(1, Height - 38));
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutChildren();
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
