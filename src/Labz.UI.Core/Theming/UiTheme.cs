using System.Drawing;

namespace Labz.UI.Core.Theming;

public sealed record UiTheme(
    Color Primary,
    Color Background,
    Color Surface,
    Color Foreground,
    Color Border,
    Color Danger,
    int CornerRadius)
{
    public static UiTheme Light { get; } = new(
        Color.FromArgb(59, 103, 235), Color.White,
        Color.FromArgb(248, 250, 252), Color.FromArgb(20, 27, 40),
        Color.FromArgb(211, 218, 230), Color.FromArgb(215, 57, 72), 8);

    public static UiTheme Dark { get; } = new(
        Color.FromArgb(111, 153, 255), Color.FromArgb(23, 28, 40),
        Color.FromArgb(36, 44, 59), Color.FromArgb(239, 243, 251),
        Color.FromArgb(76, 88, 108), Color.FromArgb(255, 132, 145), 8);
}
