using System.Drawing;
using Labz.UI.WinForms.Layout;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class FlexSizingTests
{
    [Fact]
    public void GrowDistributesExtraSpaceByWeight()
    {
        var result = LabzFlexLayoutEngine.ArrangeFlexible(
            [new Size(20, 20), new Size(20, 20)],
            new Rectangle(0, 0, 100, 30), LabzDirection.Horizontal, 0,
            LabzJustifyContent.Start, LabzAlignItems.Start, [1, 2], [1, 1]);
        Assert.Equal(40, result[0].Width);
        Assert.Equal(60, result[1].Width);
    }

    [Fact]
    public void ShrinkKeepsWidthsNonNegative()
    {
        var result = LabzFlexLayoutEngine.ArrangeFlexible(
            [new Size(80, 20), new Size(80, 20)],
            new Rectangle(0, 0, 100, 30), LabzDirection.Horizontal, 0,
            LabzJustifyContent.Start, LabzAlignItems.Start, [0, 0], [1, 1]);
        Assert.Equal(100, result.Sum(r => r.Width));
        Assert.All(result, r => Assert.True(r.Width >= 0));
    }

    [Fact]
    public void ZeroShrinkRetainsBaseSize()
    {
        var result = LabzFlexLayoutEngine.ArrangeFlexible(
            [new Size(80, 20), new Size(80, 20)],
            new Rectangle(0, 0, 100, 30), LabzDirection.Horizontal, 0,
            LabzJustifyContent.Start, LabzAlignItems.Start, [0, 0], [0, 1]);
        Assert.Equal(80, result[0].Width);
        Assert.Equal(20, result[1].Width);
    }
}
