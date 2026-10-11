using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Navigation;

/// <summary>Desktop application shell with module activity bar, collapsible sidebar and content host.</summary>
public class LabzAppShell : UserControl
{
    private readonly Panel _activityBar = new() { Dock = DockStyle.Left, Width = 58, Padding = new Padding(4, 10, 4, 4) };
    private readonly Panel _sidebar = new() { Dock = DockStyle.Left, Width = 232, AutoScroll = true, Padding = new Padding(8) };
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
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
    private bool _sidebarExpanded = true;

    public LabzAppShell()
    {
        Controls.Add(_content);
        Controls.Add(_sidebar);
        Controls.Add(_activityBar);
        _activityBar.Controls.Add(_moduleButtons);
        _sidebar.Controls.Add(_pageButtons);
        _sidebar.Controls.Add(_moduleTitle);
        _moduleButtons.SizeChanged += (_, _) => UpdateButtonWidths(_moduleButtons, 44);
        _pageButtons.SizeChanged += (_, _) => UpdateButtonWidths(_pageButtons, 190);
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
        _moduleTitle.Text = module.Title;
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
        PageRequested?.Invoke(this, new LabzNavigationEventArgs(module.Id, page.Id));
        return true;
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
                    Padding = new Padding(8, 8, 0, 0), AutoEllipsis = true
                };
                _pageButtons.Controls.Add(header);
                foreach (var page in group)
                {
                    var button = CreateButton(page.Title, 190);
                    button.Height = 36;
                    button.TextAlign = ContentAlignment.MiddleLeft;
                    button.Padding = new Padding(12, 0, 0, 0);
                    button.Click += (_, _) => PageRequested?.Invoke(this,
                        new LabzNavigationEventArgs(module.Id, page.Id));
                    _pageButtons.Controls.Add(button);
                }
            }
            ApplyTheme();
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

    private void UpdateActivitySelection()
    {
        var theme = UiThemeManager.Current;
        foreach (Button button in _moduleButtons.Controls.OfType<Button>())
            button.BackColor = string.Equals(button.Tag as string, _selectedModuleId, StringComparison.OrdinalIgnoreCase)
                ? theme.Primary : theme.Surface;
    }

    private void ApplyTheme()
    {
        if (IsDisposed) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        _content.BackColor = theme.Background;
        _sidebar.BackColor = theme.Surface;
        _activityBar.BackColor = theme.Background;
        _moduleButtons.BackColor = theme.Background;
        _pageButtons.BackColor = theme.Surface;
        _moduleTitle.BackColor = theme.Surface;
        _moduleTitle.ForeColor = theme.Foreground;
        foreach (Button button in _moduleButtons.Controls.OfType<Button>())
        {
            button.ForeColor = theme.Foreground;
            button.FlatAppearance.BorderSize = 0;
        }
        foreach (Control control in _pageButtons.Controls)
        {
            control.ForeColor = theme.Foreground;
            control.BackColor = theme.Surface;
            if (control is Button button) button.FlatAppearance.BorderSize = 0;
        }
        UpdateActivitySelection();
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
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}

public sealed class LabzNavigationEventArgs(string moduleId, string pageId) : EventArgs
{
    public string ModuleId { get; } = moduleId;
    public string PageId { get; } = pageId;
}
