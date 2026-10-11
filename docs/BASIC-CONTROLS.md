# Basic input and text controls

- `LabzCheckBox` inherits WinForms `CheckBox` and supports `Checked`,
  `ThreeState`, `CheckState`, and native events.
- `LabzRadioButton` inherits WinForms `RadioButton` and preserves
  mutual exclusion within the same parent container.
- `LabzLabel` inherits WinForms `Label` and offers `Variant`:
  `Body`, `Heading`, `Caption`, and `Muted`.

All three use Labz theme colors and are compatible with WinForms Designer.

```csharp
var title = new LabzLabel { Text = "Preferences", Variant = LabzLabelVariant.Heading };
var alerts = new LabzCheckBox { Text = "Enable alerts", Checked = true };
var daily = new LabzRadioButton { Text = "Daily" };
var weekly = new LabzRadioButton { Text = "Weekly" };
```

Manual checks are recommended for keyboard focus, high-DPI text rendering,
right-to-left layouts and accessibility tools.
