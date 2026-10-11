# Switch, ProgressBar and Separator

`LabzSwitch` derives from `CheckBox`, preserving `Checked`,
`CheckedChanged`, data binding and keyboard toggling while drawing a
theme-aware on/off track. Use it for boolean settings.

`LabzProgressBar` derives from `ProgressBar`, supporting `Minimum`,
`Maximum`, `Value`, `Variant` (Primary, Success, Danger) and
`ShowPercentage`.

`LabzSeparator` is a non-focusable divider with `Orientation`
(Horizontal/Vertical) and `Thickness`.

```csharp
var notifications = new LabzSwitch { Checked = true };
var upload = new LabzProgressBar { Value = 65, ShowPercentage = true };
var divider = new LabzSeparator { Dock = DockStyle.Top };
```

These controls support WinForms Designer properties. Validate keyboard
focus, high-DPI rendering and accessibility on Windows.
