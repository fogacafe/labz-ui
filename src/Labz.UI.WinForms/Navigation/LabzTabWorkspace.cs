using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Navigation;

/// <summary>Document-style workspace that retains page controls until explicitly closed.</summary>
[DefaultEvent(nameof(ActivePageChanged))]
public class LabzTabWorkspace : UserControl
{
    private readonly FlowLayoutPanel _tabStrip = new()
    {
        Dock = DockStyle.Top, Height = 42, WrapContents = false, AutoScroll = true,
        FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(4, 3, 4, 3)
    };
    private readonly Panel _host = new() { Dock = DockStyle.Fill };
    private readonly Dictionary<string, WorkspacePage> _pages = new(StringComparer.OrdinalIgnoreCase);
    private string? _activePageId;

    public LabzTabWorkspace()
    {
        Controls.Add(_host);
        Controls.Add(_tabStrip);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? ActivePageId => _activePageId;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyCollection<string> OpenPageIds => _pages.Keys.ToArray();

    public event EventHandler<LabzWorkspacePageEventArgs>? ActivePageChanged;
    public event EventHandler<LabzWorkspacePageEventArgs>? PageClosed;

    /// <summary>Opens a page once per key; existing pages are activated without invoking the factory.</summary>
    public bool Open(string id, string title, Func<Control> createContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(createContent);
        if (_pages.ContainsKey(id)) return Activate(id);

        var content = createContent() ?? throw new InvalidOperationException("Page factory returned null.");
        if (content.IsDisposed || content.Parent is not null)
            throw new ArgumentException("Page content must be a new unparented control.", nameof(createContent));

        var tab = new Panel { Width = 168, Height = 34, Margin = new Padding(2, 0, 2, 0) };
        var select = new Button
        {
            Dock = DockStyle.Fill, Text = title, FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true,
            AccessibleName = title, UseVisualStyleBackColor = false
        };
        var close = new Button
        {
            Dock = DockStyle.Right, Width = 30, Text = "×",
            FlatStyle = FlatStyle.Flat, AccessibleName = "Close " + title,
            UseVisualStyleBackColor = false
        };
        tab.Controls.Add(select);
        tab.Controls.Add(close);
        select.Click += (_, _) => Activate(id);
        close.Click += (_, _) => Close(id);

        content.Dock = DockStyle.Fill;
        content.Visible = false;
        _host.Controls.Add(content);
        _tabStrip.Controls.Add(tab);
        _pages.Add(id, new WorkspacePage(id, title, content, tab, select, close));
        ApplyTheme();
        return Activate(id);
    }

    public bool Activate(string id)
    {
        if (!_pages.TryGetValue(id, out var page)) return false;
        if (_activePageId == page.Id) return true;

        _host.SuspendLayout();
        try
        {
            foreach (var item in _pages.Values) item.Content.Visible = false;
            page.Content.Visible = true;
            page.Content.BringToFront();
            _activePageId = page.Id;
        }
        finally { _host.ResumeLayout(true); }

        ApplyTheme();
        ActivePageChanged?.Invoke(this, new LabzWorkspacePageEventArgs(page.Id, page.Title));
        return true;
    }

    public bool Close(string id)
    {
        if (!_pages.TryGetValue(id, out var page)) return false;
        var wasActive = _activePageId == page.Id;
        _pages.Remove(id);
        _host.Controls.Remove(page.Content);
        _tabStrip.Controls.Remove(page.Tab);
        page.Content.Dispose();
        page.Tab.Dispose();
        if (wasActive)
        {
            _activePageId = null;
            var next = _pages.Values.LastOrDefault();
            if (next is not null) Activate(next.Id);
        }
        ApplyTheme();
        PageClosed?.Invoke(this, new LabzWorkspacePageEventArgs(page.Id, page.Title));
        return true;
    }

    private void ApplyTheme()
    {
        if (IsDisposed) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        _host.BackColor = theme.Background;
        _tabStrip.BackColor = theme.Surface;
        foreach (var page in _pages.Values)
        {
            var selected = page.Id == _activePageId;
            page.Tab.BackColor = selected ? theme.Background : theme.Surface;
            foreach (var button in new[] { page.SelectButton, page.CloseButton })
            {
                button.BackColor = page.Tab.BackColor;
                button.ForeColor = theme.Foreground;
                button.FlatAppearance.BorderSize = 0;
            }
        }
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

    private sealed record WorkspacePage(
        string Id, string Title, Control Content, Panel Tab, Button SelectButton, Button CloseButton);
}

public sealed class LabzWorkspacePageEventArgs(string pageId, string title) : EventArgs
{
    public string PageId { get; } = pageId;
    public string Title { get; } = title;
}
