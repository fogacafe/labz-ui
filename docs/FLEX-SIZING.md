# FlexGrow and FlexShrink

Use `LabzStackPanel.SetFlex(child, grow, shrink)` to configure a child in a non-wrapping layout.

```csharp
var toolbar = new LabzStackPanel
{
    Direction = LabzDirection.Horizontal,
    Spacing = 12,
    Wrap = false,
    Dock = DockStyle.Top,
    Height = 64
};
var search = new LabzTextBox { Width = 240 };
var save = new LabzButton { Text = "Save" };
toolbar.Controls.Add(search);
toolbar.Controls.Add(save);
toolbar.SetFlex(search, grow: 1, shrink: 1);
toolbar.SetFlex(save, grow: 0, shrink: 0);
```

Growth distributes remaining space according to grow weights. Shrink reduces the main-axis size
proportionally to each child's basis size multiplied by its shrink factor. A factor of zero
opts out. `SetFlex` captures the child's current size as its base size.

**Initial limitations:** flexible sizing currently applies only with `Wrap = false`;
wrapped layouts retain the original layout engine. Per-child factors are configured in
code and are not serialized by the Designer yet. Min/max sizes, layout invalidation
when child preferred sizes change, and percentage bases are future work.
