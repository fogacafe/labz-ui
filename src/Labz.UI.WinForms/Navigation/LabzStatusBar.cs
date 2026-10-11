using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Navigation;

/// <summary>Theme-aware application status footer with externally managed user and tenant context.</summary>
[DefaultEvent(nameof(TenantRequested))]
public class LabzStatusBar : UserControl
{
    private readonly FlowLayoutPanel _left = new()
    {
        Dock = DockStyle.Left, AutoSize = true, WrapContents = false,
        FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10, 2, 0, 0)
    };
    private readonly FlowLayoutPanel _right = new()
    {
        Dock = DockStyle.Right, AutoSize = true, WrapContents = false,
        FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 2, 10, 0)
    };
    private readonly Label _statusLabel = new() { AutoSize = true, Padding = new Padding(0, 4, 0, 0) };
    private readonly Label _environmentLabel = new() { AutoSize = true, Padding = new Padding(8, 4, 8, 0) };
    private readonly Button _userButton = CreateContextButton();
    private readonly Button _tenantButton = CreateContextButton();
    private string? _userName;
    private string? _tenantName;
    private string? _environmentName;
    private string _statusText = "Ready";

    public LabzStatusBar()
    {
        Height = 30;
        MinimumSize = new Size(0, 30);
        Controls.Add(_left);
        Controls.Add(_right);
        _left.Controls.Add(_statusLabel);
        _right.Controls.Add(_userButton);
        _right.Controls.Add(_tenantButton);
        _right.Controls.Add(_environmentLabel);
        _userButton.Click += (_, _) => UserRequested?.Invoke(this, EventArgs.Empty);
        _tenantButton.Click += (_, _) => TenantRequested?.Invoke(this, EventArgs.Empty);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        RefreshContext();
    }

    [Category("Appearance")]
    [DefaultValue("Ready")]
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value ?? ""; RefreshContext(); }
    }

    [Category("Appearance")]
    [DefaultValue(null)]
    public string? UserName
    {
        get => _userName;
        set { _userName = value; RefreshContext(); }
    }

    [Category("Appearance")]
    [DefaultValue(null)]
    public string? TenantName
    {
        get => _tenantName;
        set { _tenantName = value; RefreshContext(); }
    }

    [Category("Appearance")]
    [DefaultValue(null)]
    public string? EnvironmentName
    {
        get => _environmentName;
        set { _environmentName = value; RefreshContext(); }
    }

    /// <summary>Raised when the user identity area is clicked. Authentication stays with the host.</summary>
    public event EventHandler? UserRequested;

    /// <summary>Raised when the tenant area is clicked. The host controls tenant switching.</summary>
    public event EventHandler? TenantRequested;

    private static Button CreateContextButton() => new()
    {
        AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlatStyle = FlatStyle.Flat, Margin = new Padding(2, 0, 2, 0),
        Padding = new Padding(6, 1, 6, 1), Cursor = Cursors.Hand,
        UseVisualStyleBackColor = false
    };

    private void RefreshContext()
    {
        _statusLabel.Text = StatusText;
        _environmentLabel.Text = string.IsNullOrWhiteSpace(EnvironmentName) ? "" : EnvironmentName;
        _environmentLabel.Visible = !string.IsNullOrWhiteSpace(EnvironmentName);
        _userButton.Text = string.IsNullOrWhiteSpace(UserName) ? "" : "👤 " + UserName;
        _userButton.Visible = !string.IsNullOrWhiteSpace(UserName);
        _tenantButton.Text = string.IsNullOrWhiteSpace(TenantName) ? "" : "▦ " + TenantName;
        _tenantButton.Visible = !string.IsNullOrWhiteSpace(TenantName);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (IsDisposed) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        _left.BackColor = theme.Surface;
        _right.BackColor = theme.Surface;
        _statusLabel.ForeColor = theme.Foreground;
        _environmentLabel.ForeColor = theme.Foreground;
        foreach (var button in new[] { _userButton, _tenantButton })
        {
            button.BackColor = theme.Surface;
            button.ForeColor = theme.Foreground;
            button.FlatAppearance.BorderSize = 0;
        }
        Invalidate();
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
