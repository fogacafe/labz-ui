using System.Drawing;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class ImageControlsTests
{
    [Fact]
    public void ContainPreservesAspectRatio()
    {
        var rect = LabzImage.CalculateImageBounds(new Size(400, 200),
            new Rectangle(0, 0, 100, 100), LabzImageFit.Contain);
        Assert.Equal(new Rectangle(0, 25, 100, 50), rect);
    }

    [Fact]
    public void CoverCropsWhilePreservingAspectRatio()
    {
        var rect = LabzImage.CalculateImageBounds(new Size(400, 200),
            new Rectangle(0, 0, 100, 100), LabzImageFit.Cover);
        Assert.Equal(new Rectangle(-50, 0, 200, 100), rect);
    }

    [Fact]
    public void FillStretchesToBounds()
    {
        var rect = LabzImage.CalculateImageBounds(new Size(400, 200),
            new Rectangle(5, 8, 100, 100), LabzImageFit.Fill);
        Assert.Equal(new Rectangle(5, 8, 100, 100), rect);
    }

    [Fact]
    public void ImageRejectsInvalidOpacity()
    {
        RunSta(() =>
        {
            using var image = new LabzImage { Opacity = .5f };
            Assert.Equal(.5f, image.Opacity);
            Assert.Throws<ArgumentOutOfRangeException>(() => image.Opacity = 1.1f);
        });
    }

    [Fact]
    public void ButtonSupportsImageOnlyPlacement()
    {
        RunSta(() =>
        {
            using var button = new LabzButton { ImagePlacement = LabzButtonImagePlacement.ImageOnly };
            Assert.Equal(LabzButtonImagePlacement.ImageOnly, button.ImagePlacement);
            Assert.Throws<ArgumentOutOfRangeException>(() => button.ImageSpacing = -1);
        });
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
