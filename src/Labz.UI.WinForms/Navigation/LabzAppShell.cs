using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Navigation;

/// <summary>Desktop application shell with module activity bar, collapsible sidebar and content host.</summary>
public class LabzAppShell : UserControl
{
    private readonly Panel _activityBar = new() { Dock = DockStyle.Left, Width = 58, Padding = new Padding(4, 10, 4, 4) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Left, Width = 232, AutoScroll = true, Padding = new Padding(8) };
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly Panel _sidebarDivider = new() { Dock = DockStyle.Right, Width = 1 };
    private readonly LabzStatusBar _statusBar = new() { Dock = DockStyle.Bottom };
    private readonly FlowLayoutPanel _moduleButtons = new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        AutoScroll = true
    };
    private readonly FlowLayoutPanel _pageButtons = new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        AutoScroll = true
    };
    private readonly Label _moduleTitle = new()
    {
        Dock = DockStyle.Top, Height = 42, TextAlign = ContentAlignment.MiddleLeft,
        Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
    };
    private readonly List<LabzModule> _modules = [];
    private string? _selectedModuleId;
    private string? _selectedPageId;
    private bool _sidebarExpanded = true;
    private LabzTabWorkspace? _workspace;
    private Func<string, string, Control>? _pageFactory;
    private bool _commandPaletteEnabled = true;
    private bool _resetWorkspaceOnTenantChange;

    public LabzAppShell()
    {
        Controls.Add(_content);
        Controls.Add(_statusBar);
        Controls.Add(_sidebar);
        Controls.Add(_activityBar);
        _activityBar.Controls.Add(_moduleButtons);
        _sidebar.Controls.Add(_pageButtons);
        _sidebar.Controls.Add(_sidebarDivider);
        _sidebar.Controls.Add(_moduleTitle);
        _moduleButtons.SizeChanged += (_, _) => UpdateButtonWidths(_moduleButtons, 44);
        _pageButtons.SizeChanged += (_, _) => UpdateButtonWidths(_pageButtons, 190);
        _statusBar.TenantChanging += OnTenantChanging;
        _statusBar.TenantChanged += OnTenantChanged;
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control.ControlCollection ContentControls => _content.Controls;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<LabzModule> Modules => _modules.AsReadOnly();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? SelectedModuleId => _selectedModuleId;

    [Category("Layout")]
    [DefaultValue(true)]
    public bool SidebarExpanded
    {
        get => _sidebarExpanded;
        set { _sidebarExpanded = value; _sidebar.Visible = value; }
    }

    /// <summary>Optional workspace connected to sidebar navigation.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LabzTabWorkspace? Workspace => _workspace;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LabzStatusBar StatusBar => _statusBar;

    /// <summary>Close open workspace pages after a successful tenant switch; dirty tabs may veto beforehand.</summary>
    [Category("Behavior")]
    [DefaultValue(false)]
    public bool ResetWorkspaceOnTenantChange
    {
        get => _resetWorkspaceOnTenantChange;
        set => _resetWorkspaceOnTenantChange = value;
    }

    private void OnTenantChanging(object? sender, LabzTenantChangingEventArgs e)
    {
        if (_resetWorkspaceOnTenantChange && _workspace is not null && !_workspace.CanCloseAll())
            e.Cancel = true;
    }

    private void OnTenantChanged(object? sender, LabzTenantChangedEventArgs e)
    {
        if (_resetWorkspaceOnTenantChange) _workspace?.CloseAllAfterApproval();
    }

    /// <summary>Connects navigation to persistent tabs; the factory is called only for new tabs.</summary>
    public void ConnectWorkspace(LabzTabWorkspace workspace, Func<string, string, Control> pageFactory)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(pageFactory);
        if (workspace.IsDisposed) throw new ObjectDisposedException(nameof(workspace));
        _workspace = workspace;
        _pageFactory = pageFactory;
        workspace.Dock = DockStyle.Fill;
        _content.Controls.Clear();
        _content.Controls.Add(workspace);
    }

    [Category("Behavior")]
    [DefaultValue(true)]
    public bool CommandPaletteEnabled
    {
        get => _commandPaletteEnabled;
        set => _commandPaletteEnabled = value;
    }

    /// <summary>Opens a global search over all registered modules and pages.</summary>
    public void ShowCommandPalette()
    {
        if (!CommandPaletteEnabled || _modules.Count == 0) return;
        using var palette = new LabzCommandPalette(_modules, (moduleId, pageId) => NavigateTo(moduleId, pageId));
        palette.ShowDialog(FindForm());
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (CommandPaletteEnabled && keyData == (Keys.Control | Keys.K))
        {
            ShowCommandPalette();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    public event EventHandler<LabzNavigationEventArgs>? PageRequested;
    public event EventHandler? SelectedModuleChanged;

    public void SetModules(IEnumerable<LabzModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);
        var items = modules.ToList();
        if (items.Any(m => string.IsNullOrWhiteSpace(m.Id)) ||
            items.Select(m => m.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Count)
            throw new ArgumentException("Module IDs must be non-empty and unique.", nameof(modules));
        _modules.Clear();
        _modules.AddRange(items);
        BuildActivityBar();
        SelectModule(_modules.FirstOrDefault()?.Id);
    }

    public bool SelectModule(string? id)
    {
        var module = _modules.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        if (module is null) return false;
        _selectedModuleId = module.Id;
        _moduleTitle.Text = "  " + module.Title;
        BuildSidebar(module);
        UpdateActivitySelection();
        SelectedModuleChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool NavigateTo(string moduleId, string pageId)
    {
        var module = _modules.FirstOrDefault(m => string.Equals(m.Id, moduleId, StringComparison.OrdinalIgnoreCase));
        var page = module?.Pages.FirstOrDefault(p => string.Equals(p.Id, pageId, StringComparison.OrdinalIgnoreCase));
        if (module is null || page is null) return false;
        SelectModule(module.Id);
        RequestPage(module, page);
        return true;
    }

    private void RequestPage(LabzModule module, LabzNavigationPage page)
    {
        _selectedModuleId = module.Id;
        _selectedPageId = page.Id;
        UpdatePageSelection();
        if (_workspace is not null && _pageFactory is not null)
        {
            var key = module.Id + "/" + page.Id;
            _workspace.Open(key, page.Title, () => _pageFactory(module.Id, page.Id));
        }
        PageRequested?.Invoke(this, new LabzNavigationEventArgs(module.Id, page.Id));
    }

    private void BuildActivityBar()
    {
        _moduleButtons.SuspendLayout();
        try
        {
            _moduleButtons.Controls.Clear();
            foreach (var module in _modules)
            {
                var button = CreateButton(string.IsNullOrWhiteSpace(module.Icon) ? module.Title[..1] : module.Icon, 44);
                button.Height = 44;
                button.Tag = module.Id;
                button.AccessibleName = module.Title;
                var tip = new ToolTip();
                tip.SetToolTip(button, module.Title);
                button.Disposed += (_, _) => tip.Dispose();
                button.Click += (_, _) => SelectModule(module.Id);
                _moduleButtons.Controls.Add(button);
            }
        }
        finally { _moduleButtons.ResumeLayout(); }
    }

    private void BuildSidebar(LabzModule module)
    {
        _pageButtons.SuspendLayout();
        try
        {
            _pageButtons.Controls.Clear();
            foreach (var group in module.Pages.GroupBy(p => p.Group))
            {
                var header = new Label
                {
                    Text = group.Key.ToUpperInvariant(), Height = 30, Width = 190,
                    Padding = new Padding(12, 8, 0, 0), AutoEllipsis = true,
                    Font = new Font(Font.FontFamily, Math.Max(8f, Font.Size - 1f), FontStyle.Bold)
                };
                _pageButtons.Controls.Add(header);
                foreach (var page in group)
                {
                    var button = CreateButton(page.Title, 190);
                    button.Height = 36;
                    button.Tag = page.Id;
                    button.TextAlign = ContentAlignment.MiddleLeft;
                    button.Padding = new Padding(16, 0, 0, 0);
                    button.AccessibleName = module.Title + " / " + page.Title;
                    button.Click += (_, _) => RequestPage(module, page);
                    _pageButtons.Controls.Add(button);
                }
            }
            ApplyTheme();
            UpdatePageSelection();
        }
        finally { _pageButtons.ResumeLayout(); }
    }

    private static Button CreateButton(string text, int width) => new()
    {
        Text = text, Width = width, Height = 36, FlatStyle = FlatStyle.Flat,
        Margin = new Padding(2), Cursor = Cursors.Hand, UseVisualStyleBackColor = false
    };

    private static void UpdateButtonWidths(FlowLayoutPanel panel, int fallback)
    {
        var width = Math.Max(fallback, panel.ClientSize.Width - 8);
        foreach (Control control in panel.Controls)
            control.Width = width;
    }

    private void UpdatePageSelection()
    {
        var theme = UiThemeManager.Current;
        foreach (var button in _pageButtons.Controls.OfType<Button>())
        {
            var selected = string.Equals(button.Tag as string, _selectedPageId, StringComparison.OrdinalIgnoreCase);
            button.BackColor = selected ? theme.SelectionSurface : theme.SidebarSurface;
            button.ForeColor = selected ? theme.Foreground : theme.MutedForeground;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = theme.SelectionSurface;
        }
    }

    private void UpdateActivitySelection()
    {
        var theme = UiThemeManager.Current;
        foreach (Button button in _moduleButtons.Controls.OfType<Button>())
            button.BackColor = string.Equals(button.Tag as string, _selectedModuleId, StringComparison.OrdinalIgnoreCase)
                ? theme.SelectionSurface : theme.Background;
    }

    private void ApplyTheme()
    {
        if (IsDisposed) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        _content.BackColor = theme.Background;
        _sidebar.BackColor = theme.SidebarSurface;
        _sidebarDivider.BackColor = theme.SubtleBorder;
        _activityBar.BackColor = theme.Background;
        _moduleButtons.BackColor = theme.Background;
        _pageButtons.BackColor = theme.SidebarSurface;
        _moduleTitle.BackColor = theme.SidebarSurface;
        _moduleTitle.ForeColor = theme.Foreground;
        foreach (Button button in _moduleButtons.Controls.OfType<Button>())
        {
            button.ForeColor = theme.Foreground;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = theme.SelectionSurface;
        }
        foreach (Control control in _pageButtons.Controls)
        {
            control.ForeColor = control is Label ? theme.MutedForeground : theme.Foreground;
            control.BackColor = theme.SidebarSurface;
            if (control is Button button)
            {
                button.FlatAppearance.BorderSize = 0;
                button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(theme.Surface, 0.08f);
                button.FlatAppearance.MouseDownBackColor = theme.SelectionSurface;
            }
        }
        UpdateActivitySelection();
        UpdatePageSelection();
        Invalidate(true);
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
            _statusBar.TenantChanging -= OnTenantChanging;
            _statusBar.TenantChanged -= OnTenantChanged;
            UiThemeManager.ThemeChanged -= OnThemeChanged;
        }
        base.Dispose(disposing);
    }
}

public sealed class LabzNavigationEventArgs(string moduleId, string pageId) : EventArgs
{
    public string ModuleId { get; } = moduleId;
    public string PageId { get; } = pageId;
}
