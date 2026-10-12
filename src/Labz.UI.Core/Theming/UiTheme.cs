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
    // Semantic surface tokens keep application shells and dense data views consistent
    // without introducing a separate hard-coded palette in each WinForms control.
    public Color ElevatedSurface => IsDark
        ? Color.FromArgb(43, 53, 71)
        : Color.White;

    public Color SidebarSurface => IsDark
        ? Color.FromArgb(31, 39, 54)
        : Color.FromArgb(242, 245, 250);

    public Color MutedForeground => IsDark
        ? Color.FromArgb(165, 177, 198)
        : Color.FromArgb(96, 110, 132);

    public Color SubtleBorder => IsDark
        ? Color.FromArgb(55, 67, 87)
        : Color.FromArgb(226, 232, 241);

    public Color SelectionSurface => IsDark
        ? Color.FromArgb(52, 75, 118)
        : Color.FromArgb(227, 237, 255);

    public bool IsDark => Background.GetBrightness() < 0.5f;

    public static UiTheme Light { get; } = new(
        Color.FromArgb(59, 103, 235), Color.White,
        Color.FromArgb(248, 250, 252), Color.FromArgb(20, 27, 40),
        Color.FromArgb(211, 218, 230), Color.FromArgb(215, 57, 72), 8);

    public static UiTheme Dark { get; } = new(
        Color.FromArgb(105, 149, 247), Color.FromArgb(23, 29, 42),
        Color.FromArgb(36, 45, 61), Color.FromArgb(232, 237, 246),
        Color.FromArgb(53, 65, 86), Color.FromArgb(255, 132, 145), 8);
}
