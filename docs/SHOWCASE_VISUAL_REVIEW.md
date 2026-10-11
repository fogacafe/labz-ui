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
