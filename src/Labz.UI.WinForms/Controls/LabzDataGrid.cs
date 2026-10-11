using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Themed DataGridView retaining native data binding, sorting and selection.</summary>
[DefaultEvent(nameof(SelectionChanged))]
public class LabzDataGrid : DataGridView
{
    private bool _stripedRows = true;
    private Font? _headerFont;

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

    protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
    {
        base.OnCellFormatting(e);
        CellFormattingHandler?.Invoke(e);
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
