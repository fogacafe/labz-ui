# Form validation (experimental)

Rules live in `Labz.UI.Validation` and do not depend on WinForms.
`LabzFormValidator` binds them to `LabzTextBox` controls.

```csharp
using Labz.UI.Validation;
using Labz.UI.WinForms.Validation;

private readonly LabzFormValidator _validation = new();

void ConfigureValidation()
{
    _validation.Register(txtTicker,
        new FieldValidator("Ticker")
            .NotEmpty("Ticker is required")
            .MustAsync((ticker, ct) => tickerService.ExistsAsync(ticker, ct),
                "Ticker not found"),
        ValidationTrigger.Blur);
}

async Task SaveAsync()
{
    var result = await _validation.ValidateAsync();
    if (!result.IsValid) return;
    // Save...
}

protected override void Dispose(bool disposing)
{
    if (disposing) _validation.Dispose();
    base.Dispose(disposing);
}
```

Triggers: `Change`, `Blur`, both, or `None` (submit-only).
Change validation currently runs for every keystroke; use Blur for expensive remote checks.
The adapter cancels superseded validations and prevents older results from repainting a field.
The consuming form should dispose the validator. Designer-generated forms should integrate
disposal into their existing Dispose implementation, rather than declaring a second override.

Planned: debounce, cross-field rules, per-form parallelism, improved error handling and richer messages.
