# Labz UI layout containers

Use `LabzPanel` for a themed surface with padding, rounded corners and an optional border.

Use `LabzStackPanel` for CSS-inspired direction, gap, justification, alignment and optional wrapping.
Use `LabzFlowPanel` when a horizontal wrapping layout is the default.

```csharp
var toolbar = new LabzStackPanel
{
    Dock = DockStyle.Top,
    Height = 64,
    Direction = LabzDirection.Horizontal,
    Spacing = 12,
    JustifyContent = LabzJustifyContent.SpaceBetween,
    AlignItems = LabzAlignItems.Center,
    Wrap = true
};
toolbar.Controls.Add(new LabzButton { Text = "Save" });
toolbar.Controls.Add(new LabzButton { Text = "Cancel" });
```

Properties are available in the WinForms Designer. The initial engine uses existing
child dimensions and margins, and lays out visible children; it does not yet support
flex-grow, shrink, order, percentage sizing, or responsive breakpoints.

`LabzPanel` is a visual surface, while `LabzStackPanel` and `LabzFlowPanel`
are layout containers. They can be nested.
