using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;

namespace Labz.UI.Showcase;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ShowcaseForm());
    }
}

public sealed class ShowcaseForm : Form
{
    private readonly LabzButton _solid = new() { Text = "Solid", Variant = LabzButtonVariant.Solid, Location = new Point(24, 58) };
    private readonly LabzButton _outline = new() { Text = "Outline", Variant = LabzButtonVariant.Outline, Location = new Point(174, 58) };
    private readonly LabzButton _ghost = new() { Text = "Ghost", Variant = LabzButtonVariant.Ghost, Location = new Point(324, 58) };
    private readonly LabzButton _disabled = new() { Text = "Disabled", Enabled = false, Location = new Point(24, 120) };
    private readonly LabzButton _large = new() { Text = "Large", ButtonSize = LabzButtonSize.Large, Location = new Point(174, 120) };
    private readonly LabzButton _toggle = new() { Text = "Toggle theme", Location = new Point(24, 200), Width = 150 };

    public ShowcaseForm()
    {
        Text = "Labz UI Showcase — Buttons";
        ClientSize = new Size(490, 270);
        Controls.AddRange([_solid, _outline, _ghost, _disabled, _large, _toggle]);
        _toggle.Click += (_, _) => UiThemeManager.Apply(
            ReferenceEquals(UiThemeManager.Current, UiTheme.Light) ? UiTheme.Dark : UiTheme.Light);
        UiThemeManager.ThemeChanged += RefreshTheme;
        RefreshTheme(this, EventArgs.Empty);
    }

    private void RefreshTheme(object? sender, EventArgs e)
    {
        BackColor = UiThemeManager.Current.Background;
        ForeColor = UiThemeManager.Current.Foreground;
        Invalidate(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= RefreshTheme;
        base.Dispose(disposing);
    }
}
