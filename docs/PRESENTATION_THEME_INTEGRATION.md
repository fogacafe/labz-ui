# Presentation theme integration

The shared Labz UI theme manager in `Labz.UI.Core.Theming` is the source of truth. Presentation does **not** create a second theme manager.

Use `ScreenThemeBinding` to synchronize `ScreenLoadingOverlay` and/or `ScreenLoadingIndicator` with `UiThemeManager.Current`. It applies the initial palette immediately and responds to `UiThemeManager.ThemeChanged`.

```csharp
var binding = new ScreenThemeBinding(overlay, indicator);
// Keep binding for the screen lifetime; call binding.Dispose() on Form.Dispose.
UiThemeManager.Apply(UiTheme.Dark);
```

The PresentationDemo deferred workspace exposes a light/dark toggle. The loading overlay, spinner accent, and indicator follow theme changes, including while loading.

The binding subscribes to a static event; **always dispose it** when the Form closes. Theme changes should be triggered on the UI thread. Other WinForms components retain their existing Core theming behavior; this package only bridges Presentation loading UI.
