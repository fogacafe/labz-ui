using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Navigation;

/// <summary>Keyboard-friendly search dialog for navigating registered module pages.</summary>
public sealed class LabzCommandPalette : Form
{
    private readonly TextBox _search = new() { Dock = DockStyle.Top, Height = 36, PlaceholderText = "Search screens..." };
    private readonly ListBox _results = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly List<PaletteEntry> _entries = [];
    private readonly Action<string, string> _navigate;

    public LabzCommandPalette(IEnumerable<LabzModule> modules, Action<string, string> navigate)
    {
        ArgumentNullException.ThrowIfNull(modules);
        _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        KeyPreview = true;
        Text = "Go to screen";
        ClientSize = new Size(470, 340);
        Controls.Add(_results);
        Controls.Add(_search);
        foreach (var module in modules)
            foreach (var page in module.Pages)
                _entries.Add(new PaletteEntry(module.Id, module.Title, page.Id, page.Title, page.Group));
        _search.TextChanged += (_, _) => Filter(_search.Text);
        _search.KeyDown += OnSearchKeyDown;
        _results.DoubleClick += (_, _) => ChooseSelected();
        _results.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            ChooseSelected();
        };
        Shown += (_, _) => _search.Focus();
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        Filter("");
    }

    /// <summary>Pure filtering logic for command palette candidates.</summary>
    public static IReadOnlyList<PaletteEntry> Search(IEnumerable<PaletteEntry> entries, string? query)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (string.IsNullOrWhiteSpace(query)) return entries.ToArray();
        var terms = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return entries.Where(e =>
        {
            var haystack = e.ModuleTitle + " " + e.PageTitle + " " + e.Group;
            return terms.All(term => haystack.Contains(term, StringComparison.OrdinalIgnoreCase));
        }).ToArray();
    }

    private void Filter(string query)
    {
        _results.BeginUpdate();
        try
        {
            _results.Items.Clear();
            foreach (var entry in Search(_entries, query))
                _results.Items.Add(entry);
            if (_results.Items.Count > 0) _results.SelectedIndex = 0;
        }
        finally { _results.EndUpdate(); }
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) { Close(); e.SuppressKeyPress = true; return; }
        if (e.KeyCode == Keys.Down && _results.Items.Count > 0)
        {
            _results.SelectedIndex = Math.Min(_results.Items.Count - 1, _results.SelectedIndex + 1);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Up && _results.Items.Count > 0)
        {
            _results.SelectedIndex = Math.Max(0, _results.SelectedIndex - 1);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            ChooseSelected();
            e.SuppressKeyPress = true;
        }
    }

    private void ChooseSelected()
    {
        if (_results.SelectedItem is not PaletteEntry entry) return;
        _navigate(entry.ModuleId, entry.PageId);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        ForeColor = theme.Foreground;
        _search.BackColor = theme.Background;
        _search.ForeColor = theme.Foreground;
        _results.BackColor = theme.Surface;
        _results.ForeColor = theme.Foreground;
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

public sealed record PaletteEntry(string ModuleId, string ModuleTitle, string PageId, string PageTitle, string Group)
{
    public override string ToString() => ModuleTitle + "  /  " + PageTitle;
}
