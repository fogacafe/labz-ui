# LabzTextBox (experimental)

A Windows Forms text input with native editing and custom rounded visual chrome.

## Properties

- `Text`, `PlaceholderText`, `ReadOnly`, `UseSystemPasswordChar`, `MaxLength`
- `ValidationState`: `None`, `Success`, `Warning`, `Error`
- `ValidationMessage` and `ShowValidationMessage`

## Example

```csharp
var ticker = new LabzTextBox
{
    Name = "Ticker",
    PlaceholderText = "Enter ticker",
    ValidationState = ValidationSeverity.Error,
    ValidationMessage = "Ticker is required"
};
```

Map server-side field errors by control name:

```csharp
new[] { ticker }.ApplyValidation(
    new ValidationResult([
        new ValidationMessage("Ticker", "Ticker is required", ValidationSeverity.Error)
    ]));
```

The control embeds a native `TextBox` for caret, clipboard, selection, and IME support. It is designed for Visual Studio Designer property editing and normal programmatic construction. This is a prototype; validation triggers, automatic resizing, accessibility review, DPI scaling and data-binding ergonomics remain future work.
