# Images and image buttons

`LabzImage` displays a caller-owned `Image` with optional rounded border,
configurable corner radius, and opacity from 0 to 1.

| Fit | Behavior |
| --- | --- |
| Contain | Entire image visible, original aspect ratio preserved |
| Cover | Fill viewport without distortion; excess image clipped |
| Fill | Stretch to viewport, potentially distorting the image |
| Center | Center and shrink only when too large |
| Original | Original pixel dimensions, centered and clipped |

```csharp
var avatar = new LabzImage
{
    Image = Properties.Resources.Avatar,
    Fit = LabzImageFit.Cover,
    CornerRadius = 24,
    ShowBorder = true,
    Opacity = 0.85f
};

var save = new LabzButton
{
    Text = "Save",
    Image = Properties.Resources.SaveIcon,
    ImagePlacement = LabzButtonImagePlacement.Left,
    ImageSize = new Size(18, 18),
    ImageSpacing = 8
};
```

`LabzButton` supports Left, Right, Top, Bottom and ImageOnly placement.
The image is not disposed by the control; the caller owns its lifetime.
Manual Designer, high-DPI and accessibility checks are recommended.
