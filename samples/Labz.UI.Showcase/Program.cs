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
    private readonly LabzButton _button = new() { Text = "Primary button", Location = new Point(24, 60) };
    private readonly LabzButton _toggle = new() { Text = "Toggle theme", Location = new Point(174, 60), Width = 140 };

    public ShowcaseForm()
    {
        Text = "Labz UI Showcase";
        ClientSize = new Size(450, 170);
        Controls.AddRange([_button, _toggle]);
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
