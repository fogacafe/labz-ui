# Interactive Showcase

Run the component gallery on Windows:

```powershell
dotnet run --project samples/Labz.UI.Showcase/Labz.UI.Showcase.csproj
```

The gallery demonstrates buttons and image buttons, text validation states,
selection controls, a local async search lookup, image fitting and opacity,
progress feedback, nested containers, accordion, typography and separators.

Use **Switch theme** in the header to compare light and dark theme rendering.
The lookup uses an in-memory city list; it does not require a backend.

The gallery creates its example bitmap in memory and does not ship external
image assets. Interactive visual verification (high DPI, resizing, keyboard
focus, scroll, accordion content and popup positioning) is recommended on
Windows. The Showcase is a demonstration rather than a screenshot regression
suite.
