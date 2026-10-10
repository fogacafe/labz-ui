using Labz.UI.Core.Theming;
using Xunit;

namespace Labz.UI.Core.Tests;

public class ThemeTests
{
    [Fact]
    public void ApplyingThemeUpdatesCurrent()
    {
        var previous = UiThemeManager.Current;
        try
        {
            UiThemeManager.Apply(UiTheme.Dark);
            Assert.Same(UiTheme.Dark, UiThemeManager.Current);
        }
        finally { UiThemeManager.Apply(previous); }
    }
}
