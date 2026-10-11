# Visual Polish Sprint — PR #87: Inputs & Interaction

## Implemented in this package

- **ComboBox:** native selection, binding and keyboard behavior retained; owner-drawn item rows adopt the active Labz palette, including selected-item colors.
- **SearchLookup:** popup results use Labz themed selection colors, alternating rows, borders and header styling; safe UI dispatch on theme changes.
- **Showcase:** prompts reviewers to open selection dropdowns in both themes.

## Known WinForms limitations and release gate

The native **DateTimePicker** month-calendar popup and **NumericUpDown** spin buttons are drawn by Windows. Setting BackColor or CalendarMonthBackground does not guarantee a themed popup under every Windows configuration. We intentionally do **not** claim those two screenshot defects are fixed by this package. Achieving complete visual parity requires a separately designed owner-drawn picker/editor (including keyboard, screen-reader, locale and DPI tests). Do not ship a half-functional calendar just to hide the white popup.

## Windows QA checklist

- ComboBox: dropdown and selected item in light/dark; arrow keys, Enter, Esc, focus, disabled state, data binding.
- SearchLookup: empty/loading/results/error states; selection, keyboard navigation, async cancellation; light/dark and high DPI.
- DatePicker: native popup contrast in light/dark, date/time localization and keyboard navigation; **known visual discrepancy**.
- NumericInput: currency/percent formats under pt-BR, negative values, min/max, blank editing, spin buttons; **known visual discrepancy**.
- Switch themes while popups are visible; close forms during pending searches; inspect for exceptions and event leaks.

CI cannot verify native popup rendering. Screenshot review on Windows remains required.
