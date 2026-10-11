using System.Text.Json;
using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Themed DataGridView retaining native data binding, sorting and selection.</summary>
[DefaultEvent(nameof(SelectionChanged))]
public class LabzDataGrid : DataGridView
{
    private bool _stripedRows = true;
    private Font? _headerFont;
    private bool _columnSelectorEnabled = true;
    private readonly Dictionary<string, bool> _groupExpanded = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<int>> _groupRows = new(StringComparer.Ordinal);
    private readonly Dictionary<int, string> _rowGroupKeys = new();

    /// <summary>Creates a one-level grouping projection for currently bound rows.</summary>
    /// <remarks>Grouping is local-only and does not support virtual mode or server paging.</remarks>
    public void GroupRows(Func<DataGridViewRow, string> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        if (VirtualMode || DataSource is null)
            throw new InvalidOperationException("Grouping requires a bound, non-virtual grid.");
        _groupRows.Clear();
        _rowGroupKeys.Clear();
        for (var index = 0; index < RowCount; index++)
        {
            var row = Rows[index];
            if (row.IsNewRow) continue;
            var key = keySelector(row) ?? string.Empty;
            if (!_groupRows.TryGetValue(key, out var members))
                _groupRows[key] = members = new List<int>();
            members.Add(index);
            _rowGroupKeys[index] = key;
        }
        ApplyGroupVisibility();
    }

    /// <summary>Gets local group keys and counts for a host-rendered group header.</summary>
    public IReadOnlyList<(string Key, int Count, bool Expanded)> GetGroups()
        => _groupRows.Select(pair => (pair.Key, pair.Value.Count, !_groupExpanded.TryGetValue(pair.Key, out var expanded) || expanded)).ToArray();

    /// <summary>Controls whether the group's data rows are visible.</summary>
    public bool SetGroupExpanded(string key, bool expanded)
    {
        if (!_groupRows.ContainsKey(key)) return false;
        _groupExpanded[key] = expanded;
        ApplyGroupVisibility();
        return true;
    }

    /// <summary>Clears grouping and restores all data rows.</summary>
    public void ClearGrouping()
    {
        _groupRows.Clear();
        _rowGroupKeys.Clear();
        _groupExpanded.Clear();
        foreach (DataGridViewRow row in Rows)
            if (!row.IsNewRow) row.Visible = true;
    }

    private void ApplyGroupVisibility()
    {
        foreach (var group in _groupRows)
        {
            var expanded = !_groupExpanded.TryGetValue(group.Key, out var state) || state;
            foreach (var index in group.Value)
            {
                if (index >= RowCount) continue;
                var row = Rows[index];
                if (!expanded && CurrentCell?.RowIndex == index)
                    CurrentCell = null;
                row.Visible = expanded;
            }
        }
    }


    private readonly Dictionary<(int Row, int Column), CellValidation> _cellValidations = new();
    private readonly Dictionary<int, CellValidation> _rowValidations = new();
    private readonly Dictionary<string, CellValidation> _keyedRowValidations = new(StringComparer.Ordinal);
    private readonly Dictionary<(string Key, string Column), CellValidation> _keyedCellValidations = new();

    /// <summary>Resolves a stable record key from a bound row. Return null for rows without an identity.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<DataGridViewRow, string?>? ValidationRowKeySelector { get; set; }

    /// <summary>Sets a validation marker that follows the record through bound sorting and filtering.</summary>
    public void SetKeyedRowValidation(string key, CellValidationSeverity severity, string message)
    {
        EnsureKeyedValidationSupported();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _keyedRowValidations[key] = new CellValidation(severity, message);
        Invalidate();
    }

    /// <summary>Sets a cell marker by stable record key and stable column Name.</summary>
    public void SetKeyedCellValidation(string key, string columnName, CellValidationSeverity severity, string message)
    {
        EnsureKeyedValidationSupported();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _keyedCellValidations[(key, columnName)] = new CellValidation(severity, message);
        Invalidate();
    }

    public bool ClearKeyedRowValidation(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var removed = _keyedRowValidations.Remove(key);
        if (removed) Invalidate();
        return removed;
    }

    public bool ClearKeyedCellValidation(string key, string columnName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        var removed = _keyedCellValidations.Remove((key, columnName));
        if (removed) Invalidate();
        return removed;
    }

    public void ClearKeyedValidations()
    {
        _keyedRowValidations.Clear();
        _keyedCellValidations.Clear();
        Invalidate();
    }

    private void EnsureKeyedValidationSupported()
    {
        if (VirtualMode || ValidationRowKeySelector is null)
            throw new InvalidOperationException("Keyed validation requires bound rows and a ValidationRowKeySelector.");
    }

    private bool TryGetValidation(int rowIndex, int columnIndex, out CellValidation? validation)
    {
        validation = null;
        if (rowIndex < 0 || rowIndex >= RowCount || columnIndex < 0 || columnIndex >= ColumnCount)
            return false;
        if (!VirtualMode && ValidationRowKeySelector is not null)
        {
            var key = ValidationRowKeySelector(Rows[rowIndex]);
            if (!string.IsNullOrEmpty(key))
            {
                if (_keyedCellValidations.TryGetValue((key, Columns[columnIndex].Name), out validation))
                    return true;
                if (_keyedRowValidations.TryGetValue(key, out validation))
                    return true;
            }
        }
        if (_cellValidations.TryGetValue((rowIndex, columnIndex), out validation)) return true;
        return _rowValidations.TryGetValue(rowIndex, out validation);
    }


    /// <summary>Optional callback when a committed cell value changes. Native CellValueChanged remains available.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DataGridViewCellEventArgs>? CellValueChangedHandler { get; set; }

    /// <summary>Optional callback for custom row styling after built-in validation styling.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DataGridViewCellFormattingEventArgs>? CustomCellStyleHandler { get; set; }

    /// <summary>Paints a whole row with an error or warning marker without modifying its values.</summary>
    public void SetRowValidation(int rowIndex, CellValidationSeverity severity, string message)
    {
        if (rowIndex < 0 || rowIndex >= RowCount) throw new ArgumentOutOfRangeException(nameof(rowIndex));
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _rowValidations[rowIndex] = new CellValidation(severity, message);
        InvalidateRow(rowIndex);
    }

    public bool ClearRowValidation(int rowIndex)
    {
        var removed = _rowValidations.Remove(rowIndex);
        if (removed && rowIndex >= 0 && rowIndex < RowCount) InvalidateRow(rowIndex);
        return removed;
    }

    public void ClearRowValidations()
    {
        _rowValidations.Clear();
        Invalidate();
    }


    /// <summary>Enables editing using native DataGridView editing semantics.</summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool EditableGrid
    {
        get => !ReadOnly;
        set => ReadOnly = !value;
    }

    /// <summary>Marks a cell with an error or warning and shows its message as a tooltip.</summary>
    public void SetCellValidation(int rowIndex, int columnIndex, CellValidationSeverity severity, string message)
    {
        if (rowIndex < 0 || rowIndex >= RowCount) throw new ArgumentOutOfRangeException(nameof(rowIndex));
        if (columnIndex < 0 || columnIndex >= ColumnCount) throw new ArgumentOutOfRangeException(nameof(columnIndex));
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _cellValidations[(rowIndex, columnIndex)] = new CellValidation(severity, message);
        InvalidateCell(columnIndex, rowIndex);
    }

    /// <summary>Clears one validation marker without modifying the cell value.</summary>
    public bool ClearCellValidation(int rowIndex, int columnIndex)
    {
        var removed = _cellValidations.Remove((rowIndex, columnIndex));
        if (removed && rowIndex >= 0 && rowIndex < RowCount && columnIndex >= 0 && columnIndex < ColumnCount)
            InvalidateCell(columnIndex, rowIndex);
        return removed;
    }

    /// <summary>Clears all visual validation markers.</summary>
    public void ClearCellValidations()
    {
        _cellValidations.Clear();
        Invalidate();
    }

    public enum CellValidationSeverity { Warning, Error }

    private sealed record CellValidation(CellValidationSeverity Severity, string Message);


    /// <summary>Allows users to drag column headers to rearrange the display order.</summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool ColumnReorderingEnabled
    {
        get => AllowUserToOrderColumns;
        set => AllowUserToOrderColumns = value;
    }

    /// <summary>Shows a column visibility selector when a column header is right-clicked.</summary>
    [Category("Behavior")]
    [DefaultValue(true)]
    public bool ColumnSelectorEnabled
    {
        get => _columnSelectorEnabled;
        set => _columnSelectorEnabled = value;
    }

    /// <summary>Creates a themed column selector menu. The caller owns the returned menu.</summary>
    public ContextMenuStrip CreateColumnSelectorMenu()
    {
        var menu = new ContextMenuStrip();
        var theme = UiThemeManager.Current;
        menu.BackColor = theme.Surface;
        menu.ForeColor = theme.Foreground;
        foreach (var column in Columns.Cast<DataGridViewColumn>().OrderBy(x => x.DisplayIndex))
        {
            var item = new ToolStripMenuItem(
                string.IsNullOrWhiteSpace(column.HeaderText) ? column.Name : column.HeaderText)
            {
                Checked = column.Visible,
                CheckOnClick = false,
                Tag = column.Name
            };
            item.Click += (_, _) =>
            {
                // Keep at least one visible column so users cannot hide the entire grid.
                if (column.Visible && Columns.Cast<DataGridViewColumn>().Count(x => x.Visible) <= 1)
                    return;
                column.Visible = !column.Visible;
                item.Checked = column.Visible;
            };
            menu.Items.Add(item);
        }
        return menu;
    }

    protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
    {
        base.OnColumnHeaderMouseClick(e);
        if (!_columnSelectorEnabled || e.Button != MouseButtons.Right) return;
        var menu = CreateColumnSelectorMenu();
        menu.Closed += (_, _) => menu.Dispose();
        var position = PointToClient(Cursor.Position);
        menu.Show(this, position);
    }

    /// <summary>Finds the next matching loaded cell, wrapping around the current row.</summary>
    /// <remarks>Searches only currently available cell values; virtual data providers should implement server-side search.</remarks>
    public bool TryFindNext(string? text, bool matchCase = false)
    {
        if (VirtualMode || string.IsNullOrWhiteSpace(text) || RowCount == 0 || ColumnCount == 0)
            return false;

        var comparison = matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
        var startRow = CurrentCell?.RowIndex ?? -1;
        var startColumn = CurrentCell?.ColumnIndex ?? -1;
        var total = (long)RowCount * ColumnCount;
        var start = startRow < 0 ? -1L : (long)startRow * ColumnCount + startColumn;

        for (long offset = 1; offset <= total; offset++)
        {
            var index = (start + offset) % total;
            var rowIndex = (int)(index / ColumnCount);
            var columnIndex = (int)(index % ColumnCount);
            var column = Columns[columnIndex];
            if (!column.Visible || !Rows[rowIndex].Visible) continue;
            var cell = Rows[rowIndex].Cells[columnIndex];
            var display = cell.FormattedValue?.ToString();
            if (display is null || display.IndexOf(text, comparison) < 0) continue;
            ClearSelection();
            CurrentCell = cell;
            cell.Selected = true;
            if (IsHandleCreated)
                FirstDisplayedScrollingRowIndex = rowIndex;
            return true;
        }

        return false;
    }

    /// <summary>Exports column order, visibility and widths as portable JSON.</summary>
    public string ExportColumnLayout()
    {
        var columns = Columns.Cast<DataGridViewColumn>()
            .Select(column => new ColumnLayout(column.Name, column.DisplayIndex, column.Visible, column.Width))
            .ToArray();
        return JsonSerializer.Serialize(columns);
    }

    /// <summary>Restores matching columns from a previously exported layout; unknown columns are ignored.</summary>
    public void ImportColumnLayout(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var saved = JsonSerializer.Deserialize<ColumnLayout[]>(json)
            ?? throw new ArgumentException("Column layout cannot be null.", nameof(json));
        var matches = saved.Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Where(item => Columns.Contains(item.Name))
            .ToArray();
        foreach (var item in matches)
        {
            var column = Columns[item.Name];
            if (column is null) continue;
            column.Visible = item.Visible;
            if (item.Width is >= 20 and <= 5000) column.Width = item.Width;
        }
        foreach (var item in matches.OrderBy(item => item.DisplayIndex))
        {
            var column = Columns[item.Name];
            if (column is not null && item.DisplayIndex >= 0)
                column.DisplayIndex = Math.Min(item.DisplayIndex, Columns.Count - 1);
        }
    }

    private sealed record ColumnLayout(string Name, int DisplayIndex, bool Visible, int Width);

    /// <summary>Optional host-provided formatting callback applied after native formatting.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DataGridViewCellFormattingEventArgs>? CellFormattingHandler { get; set; }

    /// <summary>Optional host-provided row styling callback, invoked when a row is pre-painted.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DataGridViewRowPrePaintEventArgs>? RowPrePaintHandler { get; set; }

    public LabzDataGrid()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        BorderStyle = BorderStyle.None;
        BackgroundColor = UiThemeManager.Current.Surface;
        GridColor = UiThemeManager.Current.Border;
        EnableHeadersVisualStyles = false;
        RowHeadersVisible = false;
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        MultiSelect = false;
        AllowUserToOrderColumns = true;
        ReadOnly = true;
        ShowCellToolTips = true;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        ColumnHeadersHeight = 40;
        RowTemplate.Height = 36;
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        ApplyTheme();
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(true)]
    public bool StripedRows
    {
        get => _stripedRows;
        set { _stripedRows = value; ApplyTheme(); }
    }

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool ReadOnlyGrid
    {
        get => ReadOnly;
        set => ReadOnly = value;
    }

    /// <summary>Enables or disables multi-row selection while keeping native selection semantics.</summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool MultiRowSelection
    {
        get => MultiSelect;
        set => MultiSelect = value;
    }

    /// <summary>Gets bound data items represented by the selected rows.</summary>
    public IReadOnlyList<T> GetSelectedItems<T>()
        => SelectedRows.Cast<DataGridViewRow>()
            .OrderBy(row => row.Index)
            .Select(row => row.DataBoundItem)
            .OfType<T>()
            .ToArray();

    /// <summary>Applies a filter to a BindingSource; provider support is determined by its underlying list.</summary>
    public bool TryApplyFilter(string? filter)
    {
        if (DataSource is not BindingSource source || !source.SupportsFiltering) return false;
        source.Filter = filter;
        return true;
    }

    /// <summary>Clears a supported BindingSource filter.</summary>
    public bool TryClearFilter() => TryApplyFilter(null);

    /// <summary>Configures unbound virtual rows. Handle CellValueNeeded to supply values on demand.</summary>
    /// <remarks>Virtual mode cannot be enabled while a bound DataSource is attached.</remarks>
    public void ConfigureVirtualRows(int rowCount)
    {
        if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
        if (DataSource is not null)
            throw new InvalidOperationException("Remove the DataSource before enabling virtual rows.");
        VirtualMode = true;
        RowCount = rowCount;
    }

    /// <summary>Updates the virtual row count without resetting columns or event handlers.</summary>
    public void UpdateVirtualRowCount(int rowCount)
    {
        if (!VirtualMode || DataSource is not null)
            throw new InvalidOperationException("Configure unbound virtual mode first.");
        if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
        RowCount = rowCount;
    }

    /// <summary>Exits virtual mode and clears its unbound rows.</summary>
    public void ClearVirtualRows()
    {
        if (!VirtualMode || DataSource is not null) return;
        RowCount = 0;
        VirtualMode = false;
    }

    /// <summary>Convenience helper for bound lists; DataSource remains the native binding API.</summary>
    public void Bind<T>(BindingList<T> items) => DataSource = items;

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackgroundColor = theme.Surface;
        BackColor = theme.Surface;
        ForeColor = theme.Foreground;
        GridColor = theme.Border;
        DefaultCellStyle.BackColor = theme.Surface;
        DefaultCellStyle.ForeColor = theme.Foreground;
        DefaultCellStyle.SelectionBackColor = theme.Primary;
        DefaultCellStyle.SelectionForeColor = Color.White;
        DefaultCellStyle.Padding = new Padding(8, 4, 8, 4);
        AlternatingRowsDefaultCellStyle.BackColor = StripedRows
            ? Color.FromArgb(
                (theme.Surface.R * 9 + theme.Background.R) / 10,
                (theme.Surface.G * 9 + theme.Background.G) / 10,
                (theme.Surface.B * 9 + theme.Background.B) / 10)
            : theme.Surface;
        AlternatingRowsDefaultCellStyle.ForeColor = theme.Foreground;
        AlternatingRowsDefaultCellStyle.SelectionBackColor = theme.Primary;
        AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        ColumnHeadersDefaultCellStyle.BackColor = theme.Background;
        ColumnHeadersDefaultCellStyle.ForeColor = theme.Foreground;
        ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.Background;
        ColumnHeadersDefaultCellStyle.SelectionForeColor = theme.Foreground;
        UpdateHeaderFont();
        RowHeadersDefaultCellStyle.BackColor = theme.Background;
        RowHeadersDefaultCellStyle.ForeColor = theme.Foreground;
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (ColumnHeadersDefaultCellStyle is not null)
            UpdateHeaderFont();
    }

    protected override void OnCellToolTipTextNeeded(DataGridViewCellToolTipTextNeededEventArgs e)
    {
        base.OnCellToolTipTextNeeded(e);
        if (TryGetValidation(e.RowIndex, e.ColumnIndex, out var validation))
            e.ToolTipText = validation!.Message;
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        base.OnCellPainting(e);
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (!TryGetValidation(e.RowIndex, e.ColumnIndex, out var validation)) return;
        var color = validation!.Severity == CellValidationSeverity.Error
            ? Color.FromArgb(205, 65, 65)
            : Color.FromArgb(210, 150, 35);
        using var pen = new Pen(color, 2f);
        var bounds = e.CellBounds;
        if (bounds.Width > 4 && bounds.Height > 4)
            e.Graphics.DrawLine(pen, bounds.Left + 3, bounds.Bottom - 3, bounds.Right - 3, bounds.Bottom - 3);
    }

    protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
    {
        base.OnCellFormatting(e);
        CellFormattingHandler?.Invoke(e);
        CustomCellStyleHandler?.Invoke(e);
    }

    protected override void OnCellValueChanged(DataGridViewCellEventArgs e)
    {
        base.OnCellValueChanged(e);
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            CellValueChangedHandler?.Invoke(e);
    }

    protected override void OnRowPrePaint(DataGridViewRowPrePaintEventArgs e)
    {
        base.OnRowPrePaint(e);
        RowPrePaintHandler?.Invoke(e);
    }

    private void UpdateHeaderFont()
    {
        var previous = _headerFont;
        _headerFont = new Font(Font, FontStyle.Bold);
        ColumnHeadersDefaultCellStyle.Font = _headerFont;
        previous?.Dispose();
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
            UiThemeManager.ThemeChanged -= OnThemeChanged;
            _headerFont?.Dispose();
        }
        base.Dispose(disposing);
    }
}
