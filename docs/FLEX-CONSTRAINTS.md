# Flex layout constraints and Designer properties

`LabzStackPanel` now supports per-child `FlexGrow` and `FlexShrink` as
WinForms extender properties. Select a child inside a LabzStackPanel in the
Designer and configure its factors in the Properties grid. The existing
`SetFlex(child, grow, shrink)` API remains available.

The layout engine applies weights separately to each wrapped line, distributes
unused space while respecting each child's `MinimumSize` and `MaximumSize`,
and preserves a stable original basis for resizing.

Example:

```csharp
toolbar.Wrap = true;
toolbar.SetFlex(search, grow: 1, shrink: 1);
toolbar.SetFlex(save, grow: 0, shrink: 0);
search.MinimumSize = new Size(140, 32);
save.MaximumSize = new Size(150, 40);
```

**Remaining considerations:** the initial basis is captured on first layout;
changing the desired basis later needs a dedicated API. Designer integration
and DPI scaling need interactive Windows verification. Percentage-based sizing
and responsive breakpoints are not yet implemented.
