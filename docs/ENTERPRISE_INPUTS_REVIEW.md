# Enterprise inputs — review package

This package hardens the existing WinForms input controls ahead of visual Showcase review.

- `LabzNumericInput` exposes `AllowEmptyInput` (default false). When enabled, validation does not overwrite a blank editor with a formatted number. **Important:** this does not make the underlying `NumericUpDown.Value` nullable; it remains the last committed decimal. Business models requiring nullable values should bind a separate nullable model property and explicitly interpret blank text.
- `LabzNumericInput` and `LabzDatePicker` guard theme-event UI marshaling when handles are being destroyed.
- The Showcase Advanced Inputs gallery displays the opt-in blank-input variant.

Manual Windows QA: inspect currency/percent formatting under pt-BR, type and clear numeric values, switch themes while editing, open/close date pickers, and inspect DPI/scaling. This is incremental hardening, not a replacement of the native WinForms input behavior.
