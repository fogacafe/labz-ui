namespace Labz.UI.Core.Theming;

public static class UiThemeManager
{
    private static UiTheme _current = UiTheme.Light;

    public static UiTheme Current => _current;
    public static event EventHandler? ThemeChanged;

    public static void Apply(UiTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        _current = theme;
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }
}
