# Validation reliability

The independent `Labz.UI.Validation` project can be tested without a Windows UI thread.

The reliability tests cover:

- deterministic rule ordering;
- propagation of cancellation tokens to asynchronous rules;
- cancellation between sequential rules;
- error severity and field name mapping;
- propagation of unexpected asynchronous rule failures.

## Current boundary

The `LabzFormValidator` adapter owns UI event subscriptions and cancellation of superseded
requests. It is not covered by these headless tests because it depends on WinForms.
The next test layer should run on a Windows STA thread and exercise the adapter directly,
including rapid typing, blur, disposal, and exceptions from asynchronous services.

**Do not treat a passing CI as proof of Designer compatibility or visual accessibility.**
Those need explicit Windows testing.
