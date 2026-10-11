# Input chrome theme QA

This PR targets the ComboBox and SearchLookup native border/button contrast visible in dark-theme screenshots.

- ComboBox: flat native selection control hosted in a themed frame; owner-drawn popup items retained.
- SearchLookup: borderless search text field, flat search action and shared themed frame; async lookup results and keyboard behavior retained.

Windows manual checks: dark/light themes, focus and disabled states, dropdown arrow, selected items, keyboard Up/Down/Enter/Escape, popup selection, DPI 100/125/150%, and long text. Native Windows arrow/button painting can still vary with OS theme; inspect before considering this fully resolved.

DatePicker native calendar and NumericInput spinner chrome remain separate issues, not fixed by this PR.
