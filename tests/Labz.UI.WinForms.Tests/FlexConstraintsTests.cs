using System.Drawing;
using Labz.UI.WinForms.Layout;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class FlexConstraintsTests
{
    [Fact]
    public void WrappedLinesGrowIndependently()
    {
        var rectangles = LabzFlexLayoutEngine.ArrangeConstrained(
            [new Size(60, 20), new Size(60, 20)],
            new Rectangle(0, 0, 100, 100), LabzDirection.Horizontal, 10, true,
            LabzJustifyContent.Start, LabzAlignItems.Start,
            [1, 1], [1, 1], [Size.Empty, Size.Empty], [Size.Empty, Size.Empty]);
        Assert.Equal(100, rectangles[0].Width);
        Assert.Equal(100, rectangles[1].Width);
        Assert.Equal(30, rectangles[1].Top);
    }

    [Fact]
    public void GrowthRedistributesAfterMaximumReached()
    {
        var rectangles = LabzFlexLayoutEngine.ArrangeConstrained(
            [new Size(20, 10), new Size(20, 10)],
            new Rectangle(0, 0, 100, 20), LabzDirection.Horizontal, 0, false,
            LabzJustifyContent.Start, LabzAlignItems.Start,
            [1, 1], [1, 1], [Size.Empty, Size.Empty],
            [new Size(30, 0), Size.Empty]);
        Assert.Equal(30, rectangles[0].Width);
        Assert.Equal(70, rectangles[1].Width);
    }

    [Fact]
    public void ShrinkStopsAtMinimum()
    {
        var rectangles = LabzFlexLayoutEngine.ArrangeConstrained(
            [new Size(80, 10), new Size(80, 10)],
            new Rectangle(0, 0, 100, 20), LabzDirection.Horizontal, 0, false,
            LabzJustifyContent.Start, LabzAlignItems.Start,
            [0, 0], [1, 1], [new Size(70, 0), Size.Empty],
            [Size.Empty, Size.Empty]);
        Assert.Equal(70, rectangles[0].Width);
        Assert.Equal(30, rectangles[1].Width);
    }
}
