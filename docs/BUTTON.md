# LabzButton

The first experimental themed control. It supports Designer-friendly public properties and standard WinForms button events.

## Properties

- `Variant`: `Solid`, `Outline`, `Ghost`
- `ButtonSize`: `Small`, `Medium`, `Large`
- `Outline`: compatibility alias for the original prototype

## Usage

```csharp
var save = new LabzButton
{
    Text = "Save",
    Variant = LabzButtonVariant.Solid,
    ButtonSize = LabzButtonSize.Large
};
save.Click += (_, _) => Save();
```

## Limitations

This is an early drawing prototype. Hover, pressed, disabled and focus states exist, but final color tokens, keyboard visual testing, high-DPI validation, accessibility audits and fluent builders remain on the roadmap.
