# Labz UI — Windows Showcase QA checklist

## Automated smoke coverage
Run `dotnet test Labz.UI.slnx --configuration Release` on Windows.
The STA smoke tests exercise theme switching, controls initialization,
DataGrid binding updates, selection, and advanced inputs.

## Manual visual checks (not covered by CI)
1. Launch `dotnet run --project samples/Labz.UI.Showcase` on Windows.
2. Switch Light/Dark several times; verify no stale colors, clipping or flicker.
3. Resize window from 850x650 to maximized; inspect every gallery card and scroll.
4. Test at Windows display scaling 100%, 125%, 150%, 200%; inspect fonts, icons,
   header height, focus indicators, popups and tooltip positioning.
5. Keyboard-only: Tab, Shift+Tab, Enter, Space, arrows, Ctrl+Tab for tabs;
   Escape for popups, menus and dialogs.
6. DataGrid: selection, keyboard arrows, sort by header with sortable data source,
   editing, BindingSource refresh, alternating rows and empty binding.
7. NumericInput: pt-BR/en-US currency, negative values, percent input, min/max,
   paste, decimal separator, arrow keys and spinner buttons.
8. DatePicker: min/max, date/time, keyboard, locale and calendar dropdown.
9. Spinner: start/stop, visibility changes, form close; no timer leaks.
10. Designer: drag controls from toolbox, set properties, save, reopen and
    build generated InitializeComponent code.

## Known limitations
- CI can assert behavior but cannot establish visual correctness without
  actual screenshots / manual inspection.
- Native DataGridView sorting requires a sortable data source.
- Manual Designer and DPI checks are required before calling this a UI release.
