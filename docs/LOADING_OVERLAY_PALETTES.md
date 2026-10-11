# Loading overlay palettes

`ScreenLoadingOverlay` supports `UseLightPalette()`, `UseDarkPalette()` and `ApplyPalette(background, foreground, accent)` for dynamic theme changes. The palette affects the background, message and spinner colors; changing it does not restart loading operations.

This PR deliberately avoids inventing a dependency on a theme manager. A follow-up can bridge the real Labz theme service to `ApplyPalette` after confirming its public API. Current palettes are explicit defaults, not automatic application theme detection. Test light/dark contrast and live theme switching on Windows.
