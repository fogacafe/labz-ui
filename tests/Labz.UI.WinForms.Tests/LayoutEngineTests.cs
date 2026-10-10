using System.Drawing;
using Labz.UI.WinForms.Layout;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class LayoutEngineTests
{
    [Fact]
    public void HorizontalSpaceBetweenDistributesRemainingWidth()
    {
        var bounds = LabzFlexLayoutEngine.Arrange(
            [new Size(20, 10), new Size(20, 10)], new Rectangle(0, 0, 100, 30),
            LabzDirection.Horizontal, 10, false,
            LabzJustifyContent.SpaceBetween, LabzAlignItems.Center);
        Assert.Equal(new Rectangle(0, 10, 20, 10), bounds[0]);
        Assert.Equal(new Rectangle(80, 10, 20, 10), bounds[1]);
    }

    [Fact]
    public void WrapMovesOverflowingItemToNextLine()
    {
        var bounds = LabzFlexLayoutEngine.Arrange(
            [new Size(45, 10), new Size(45, 10), new Size(45, 10)],
            new Rectangle(0, 0, 100, 100),
            LabzDirection.Horizontal, 10, true,
            LabzJustifyContent.Start, LabzAlignItems.Start);
        Assert.Equal(new Point(0, 20), bounds[2].Location);
    }

    [Fact]
    public void VerticalStretchFillsAvailableWidth()
    {
        var bounds = LabzFlexLayoutEngine.Arrange(
            [new Size(20, 10)], new Rectangle(5, 5, 100, 80),
            LabzDirection.Vertical, 0, false,
            LabzJustifyContent.Start, LabzAlignItems.Stretch);
        Assert.Equal(new Rectangle(5, 5, 100, 10), bounds[0]);
    }

    [Fact]
    public void EmptyAndNegativeGapAreHandled()
    {
        Assert.Empty(LabzFlexLayoutEngine.Arrange([], Rectangle.Empty,
            LabzDirection.Horizontal, 0, true, LabzJustifyContent.Start, LabzAlignItems.Start));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LabzFlexLayoutEngine.Arrange([new Size(10, 10)], Rectangle.Empty,
                LabzDirection.Horizontal, -1, false, LabzJustifyContent.Start, LabzAlignItems.Start));
    }
}
