using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Accessible placeholder for an empty search or content view.</summary>
public class LabzEmptyState : UserControl
{
    private readonly LabzLabel _heading = new() { Variant = LabzLabelVariant.Heading, AutoSize = false, Dock = DockStyle.Top, Height = 32, TextAlign = ContentAlignment.MiddleCenter };
    private readonly LabzLabel _description = new() { Variant = LabzLabelVariant.Muted, AutoSize = false, Dock = DockStyle.Top, Height = 38, TextAlign = ContentAlignment.MiddleCenter };

    public LabzEmptyState()
    {
        Size = new Size(320, 140);
        Padding = new Padding(12, 8, 12, 8);
        Controls.Add(_description);
        Controls.Add(_heading);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    [Category("Appearance")]
    [DefaultValue("No results")]
    public string Heading
    {
        get => _heading.Text;
        set => _heading.Text = value;
    }

    [Category("Appearance")]
    [DefaultValue("")]
    public string Description
    {
        get => _description.Text;
        set => _description.Text = value;
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
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
