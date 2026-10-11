# Showcase visual review

The interactive Showcase is the visual acceptance environment for Labz UI. Build and run on Windows with the .NET 10 SDK:

```powershell
dotnet restore Labz.UI.slnx
dotnet run --project samples/Labz.UI.Showcase/Labz.UI.Showcase.csproj
```

Use the **Switch theme** button to review light and dark variants. Test the Trading Desk Demo and DataGrid v1 windows using the buttons in the header.

## Suggested review order

1. Buttons, typography, spacing, surfaces and borders.
2. Inputs (text, numeric, date, combo, search), validation states, keyboard navigation.
3. Navigation, app shell, tab workspace, focus and active states.
4. Data grid: row density, headers, selection, sorting, validation markers.
5. Feedback: spinner, progress, dialogs, toast, loading and empty states.
6. Window resizing and 100%, 125%, 150% Windows scaling.

For each issue, send a screenshot with the section name, light/dark mode, display scaling, expected appearance, and what looks wrong. We can fix groups of related issues in one PR.

**Release gate:** Windows visual inspection and keyboard/accessibility checks remain manual. Green CI confirms build/tests, not pixel-level visual quality.

## Visual Polish Sprint — final release gate

The three integrated packages are: #87 (selection and lookup), #88 (tabs and content surfaces), #89 (Showcase review). The first two packages include known native WinForms visual limitations; do not call them resolved without reviewing them on Windows.

### Reviewer sign-off matrix

| Area | Light | Dark | 125% DPI | 150% DPI | Keyboard |
| --- | --- | --- | --- | --- | --- |
| ComboBox and SearchLookup popup | [ ] | [ ] | [ ] | [ ] | [ ] |
| DatePicker calendar (native limitation) | [ ] | [ ] | [ ] | [ ] | [ ] |
| NumericInput spinner (native limitation) | [ ] | [ ] | [ ] | [ ] | [ ] |
| Tabs and tab workspace | [ ] | [ ] | [ ] | [ ] | [ ] |
| Card and EmptyState (no clipping) | [ ] | [ ] | [ ] | [ ] | [ ] |
| Showcase gallery and header | [ ] | [ ] | [ ] | [ ] | [ ] |
| Trading Desk and DataGrid demos | [ ] | [ ] | [ ] | [ ] | [ ] |

Capture screenshots of any mismatch, including Windows version, theme, display scaling, and section title. Also verify popup opening near screen edges, resizing, Tab/Ctrl+Tab, Esc/Enter, and disabled controls.

### Release decision

**Candidate only; not a published release.** A green build is necessary but insufficient. Do not publish or tag a stable package until the Windows visual sign-off above and any remaining accessibility issues are resolved. Native DatePicker and NumericInput chrome may require dedicated replacements or documented exceptions.
