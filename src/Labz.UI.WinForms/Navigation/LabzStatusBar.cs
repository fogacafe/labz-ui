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
    private readonly Label _environmentLabel = new() { AutoSize = true, Padding = new Padding(8, 4, 8, 0), Margin = new Padding(2, 0, 4, 0) };
    private readonly Panel _divider = new() { Dock = DockStyle.Top, Height = 1 };
    private readonly Button _userButton = CreateContextButton();
    private readonly Button _tenantButton = CreateContextButton();
    private LabzUserContext? _userContext;
    private LabzTenantContext? _tenantContext;
    private readonly List<LabzTenantContext> _availableTenants = [];
    private bool _tenantSelectorEnabled;
    private bool _isTenantSwitching;
    private Func<LabzTenantContext?, LabzTenantContext, CancellationToken, Task<bool>>? _tenantSwitchHandler;
    private string? _userName;
    private string? _tenantName;
    private string? _environmentName;
    private string _statusText = "Ready";

    public LabzStatusBar()
    {
        Height = 32;
        MinimumSize = new Size(0, 32);
        Controls.Add(_left);
        Controls.Add(_right);
        Controls.Add(_divider);
        _left.Controls.Add(_statusLabel);
        _right.Controls.Add(_userButton);
        _right.Controls.Add(_tenantButton);
        _right.Controls.Add(_environmentLabel);
        _userButton.Click += (_, _) =>
        {
            UserContextRequested?.Invoke(this, new LabzContextRequestedEventArgs<LabzUserContext>(_userContext));
            UserRequested?.Invoke(this, EventArgs.Empty);
        };
        _tenantButton.Click += (_, _) =>
        {
            TenantContextRequested?.Invoke(this, new LabzContextRequestedEventArgs<LabzTenantContext>(_tenantContext));
            TenantRequested?.Invoke(this, EventArgs.Empty);
            if (_tenantSelectorEnabled) ShowTenantMenu();
        };
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
        set { _userName = value; _userContext = null; RefreshContext(); }
    }

    [Category("Appearance")]
    [DefaultValue(null)]
    public string? TenantName
    {
        get => _tenantName;
        set { _tenantName = value; _tenantContext = null; RefreshContext(); }
    }

    /// <summary>Typed identity context; takes precedence over legacy UserName.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LabzUserContext? UserContext
    {
        get => _userContext;
        set { _userContext = value; _userName = value?.DisplayName; RefreshContext(); }
    }

    /// <summary>Typed tenant context; takes precedence over legacy TenantName.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LabzTenantContext? TenantContext
    {
        get => _tenantContext;
        set { _tenantContext = value; _tenantName = value?.DisplayName; RefreshContext(); }
    }

    /// <summary>Tenants offered to the user; access control remains with the host.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<LabzTenantContext> AvailableTenants => _availableTenants.AsReadOnly();

    [Category("Behavior")]
    [DefaultValue(false)]
    public bool TenantSelectorEnabled
    {
        get => _tenantSelectorEnabled;
        set => _tenantSelectorEnabled = value;
    }

    /// <summary>Replaces the list of selectable tenants without changing the active tenant.</summary>
    public void SetAvailableTenants(IEnumerable<LabzTenantContext> tenants)
    {
        ArgumentNullException.ThrowIfNull(tenants);
        var items = tenants.ToArray();
        if (items.Any(t => t is null || string.IsNullOrWhiteSpace(t.Id) || string.IsNullOrWhiteSpace(t.DisplayName)) ||
            items.Select(t => t.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != items.Length)
            throw new ArgumentException("Tenants require non-empty unique IDs and display names.", nameof(tenants));
        _availableTenants.Clear();
        _availableTenants.AddRange(items);
    }

    /// <summary>Host-owned async authorization/loading callback. Return true only after the switch is committed.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<LabzTenantContext?, LabzTenantContext, CancellationToken, Task<bool>>? TenantSwitchHandler
    {
        get => _tenantSwitchHandler;
        set => _tenantSwitchHandler = value;
    }

    [Browsable(false)]
    public bool IsTenantSwitching => _isTenantSwitching;

    /// <summary>Requests a tenant change. Returns false when unavailable, canceled or unchanged.</summary>
    public bool TrySelectTenant(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (_isTenantSwitching || _tenantSwitchHandler is not null) return false;
        var candidate = _availableTenants.FirstOrDefault(t => string.Equals(t.Id, tenantId, StringComparison.OrdinalIgnoreCase));
        if (candidate is null || string.Equals(_tenantContext?.Id, candidate.Id, StringComparison.OrdinalIgnoreCase)) return false;
        var args = new LabzTenantChangingEventArgs(_tenantContext, candidate);
        TenantChanging?.Invoke(this, args);
        if (args.Cancel) return false;
        TenantContext = candidate;
        TenantChanged?.Invoke(this, new LabzTenantChangedEventArgs(candidate));
        return true;
    }

    /// <summary>Runs the host's asynchronous switch before committing the visible tenant.</summary>
    public async Task<bool> TrySelectTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (_isTenantSwitching || cancellationToken.IsCancellationRequested) return false;
        var candidate = _availableTenants.FirstOrDefault(t => string.Equals(t.Id, tenantId, StringComparison.OrdinalIgnoreCase));
        if (candidate is null || string.Equals(_tenantContext?.Id, candidate.Id, StringComparison.OrdinalIgnoreCase)) return false;
        var args = new LabzTenantChangingEventArgs(_tenantContext, candidate);
        TenantChanging?.Invoke(this, args);
        if (args.Cancel) return false;

        var previousStatus = StatusText;
        _isTenantSwitching = true;
        _tenantButton.Enabled = false;
        StatusText = "Switching tenant…";
        try
        {
            // Capture the UI synchronization context; host callback owns transactional semantics.
            var accepted = _tenantSwitchHandler is null ||
                await _tenantSwitchHandler(_tenantContext, candidate, cancellationToken);
            if (!accepted || cancellationToken.IsCancellationRequested) return false;
            TenantContext = candidate;
            TenantChanged?.Invoke(this, new LabzTenantChangedEventArgs(candidate));
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            TenantSwitchFailed?.Invoke(this, new LabzTenantSwitchFailedEventArgs(candidate, ex));
            return false;
        }
        finally
        {
            StatusText = previousStatus;
            _isTenantSwitching = false;
            _tenantButton.Enabled = true;
        }
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

    /// <summary>Typed user context snapshot on user button click.</summary>
    public event EventHandler<LabzContextRequestedEventArgs<LabzUserContext>>? UserContextRequested;

    /// <summary>Typed tenant context snapshot on tenant button click.</summary>
    public event EventHandler<LabzContextRequestedEventArgs<LabzTenantContext>>? TenantContextRequested;

    /// <summary>Raised before the selected tenant changes; set Cancel to reject the request.</summary>
    public event EventHandler<LabzTenantChangingEventArgs>? TenantChanging;

    /// <summary>Raised after the active tenant has changed.</summary>
    public event EventHandler<LabzTenantChangedEventArgs>? TenantChanged;

    /// <summary>Raised when the host switch callback fails; the displayed tenant is unchanged.</summary>
    public event EventHandler<LabzTenantSwitchFailedEventArgs>? TenantSwitchFailed;

    private void ShowTenantMenu()
    {
        if (_availableTenants.Count == 0 || _isTenantSwitching) return;
        using var menu = new ContextMenuStrip();
        foreach (var tenant in _availableTenants)
        {
            var item = new ToolStripMenuItem(tenant.DisplayName)
            {
                Checked = string.Equals(tenant.Id, _tenantContext?.Id, StringComparison.OrdinalIgnoreCase),
                Enabled = !string.Equals(tenant.Id, _tenantContext?.Id, StringComparison.OrdinalIgnoreCase)
            };
            item.Click += async (_, _) => await TrySelectTenantAsync(tenant.Id);
            menu.Items.Add(item);
        }
        menu.Show(_tenantButton, new Point(0, _tenantButton.Height));
    }

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
        _environmentLabel.Font = new Font(Font, FontStyle.Bold);
        _environmentLabel.BackColor = GetEnvironmentColor(EnvironmentName);
        _userButton.Text = string.IsNullOrWhiteSpace(UserName) ? "" : "●  " + UserName;
        _userButton.Visible = !string.IsNullOrWhiteSpace(UserName);
        _tenantButton.Text = string.IsNullOrWhiteSpace(TenantName) ? "" : "◫  " + TenantName;
        _tenantButton.Visible = !string.IsNullOrWhiteSpace(TenantName);
        ApplyTheme();
    }

    /// <summary>Semantic environment accent; neutral for unknown environments.</summary>
    public static Color GetEnvironmentColor(string? environment) => environment?.Trim().ToUpperInvariant() switch
    {
        "PROD" or "PRODUCTION" or "PRODUÇÃO" => Color.FromArgb(170, 64, 64),
        "UAT" or "STAGING" or "HML" or "HOMOLOG" => Color.FromArgb(176, 125, 39),
        "DEV" or "DEVELOPMENT" => Color.FromArgb(58, 113, 161),
        _ => Color.FromArgb(100, 104, 110)
    };

    private void ApplyTheme()
    {
        if (IsDisposed) return;
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        _left.BackColor = theme.Surface;
        _right.BackColor = theme.Surface;
        _divider.BackColor = ControlPaint.Dark(theme.Surface, 0.08f);
        _statusLabel.ForeColor = theme.Foreground;
        _environmentLabel.ForeColor = Color.White;
        _environmentLabel.BackColor = GetEnvironmentColor(EnvironmentName);
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

/// <summary>Cancelable request to change the selected tenant.</summary>
public sealed class LabzTenantChangingEventArgs(LabzTenantContext? current, LabzTenantContext requested) : CancelEventArgs
{
    public LabzTenantContext? Current { get; } = current;
    public LabzTenantContext Requested { get; } = requested;
}

/// <summary>Notification that the selected tenant changed.</summary>
public sealed class LabzTenantChangedEventArgs(LabzTenantContext tenant) : EventArgs
{
    public LabzTenantContext Tenant { get; } = tenant;
}

/// <summary>Failure during a host-managed asynchronous tenant switch.</summary>
public sealed class LabzTenantSwitchFailedEventArgs(LabzTenantContext requested, Exception exception) : EventArgs
{
    public LabzTenantContext Requested { get; } = requested;
    public Exception Exception { get; } = exception;
}
