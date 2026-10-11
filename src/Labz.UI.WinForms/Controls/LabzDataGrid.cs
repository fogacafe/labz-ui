using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Themed DataGridView retaining native data binding, sorting and selection.</summary>
[DefaultEvent(nameof(SelectionChanged))]
public class LabzDataGrid : DataGridView
{
    private bool _stripedRows = true;

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
        ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        RowHeadersDefaultCellStyle.BackColor = theme.Background;
        RowHeadersDefaultCellStyle.ForeColor = theme.Foreground;
        Invalidate();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (ColumnHeadersDefaultCellStyle is not null)
            ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke((Action)ApplyTheme);
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
