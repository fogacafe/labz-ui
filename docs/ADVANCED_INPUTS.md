# Advanced inputs

`LabzDatePicker` extends native `DateTimePicker`, preserving min/max dates,
value binding, keyboard input, calendar popup and short date format. Set
`ShowTime = true` for localized date/time display.

`LabzNumericInput` extends native `NumericUpDown`, using `decimal` values
for exact currency and percent input. Its `NumericFormat` is `Number`,
`Currency` or `Percent`; set `CultureName = "pt-BR"` to use a specific
culture, or leave empty for the current culture.

```csharp
var date = new LabzDatePicker { ShowTime = true };
var price = new LabzNumericInput
{
    NumericFormat = LabzNumericFormat.Currency,
    CultureName = "pt-BR",
    DecimalPlaces = 2,
    Value = 1234.56m
};
var percent = new LabzNumericInput
{
    NumericFormat = LabzNumericFormat.Percent,
    Value = 25m
};
```

**Percent semantics:** the decimal value `25m` means **25%**, not `0.25`.
Use explicit conversion at integration boundaries if the domain stores ratios.
Both inputs retain native WinForms keyboard, range and validation semantics.
