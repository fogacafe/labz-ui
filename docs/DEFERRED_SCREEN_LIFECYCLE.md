# Nonblocking screen opening — experimental

`DeferredScreenFactory.Open<TView, TPresenter>()` returns a Form immediately and starts initialization asynchronously after yielding once to the UI message loop. The caller can host the Form in a tab and show a loading overlay while observing `screen.Initialization`.

```csharp
var screen = factory.Open<TradeForm, TradePresenter>();
// Attach screen.View to a tab, then observe completion:
try { await screen.Initialization; }
catch (OperationCanceledException) { /* Tab closed or navigation cancelled */ }
catch (Exception ex) { /* Show a themed screen-level error */ }
// When closing the tab:
await screen.DisposeAsync();
```

This is an additive proof of concept. Existing `AsyncScreenFactory` remains unchanged. Each screen has an independent cancellation source. Disposal requests cancellation and awaits initialization before closing the presenter and disposing the Form/scope.

**Important:** Presenter initialization must honor cancellation; otherwise closing waits for it indefinitely. All Form access must occur on the WinForms UI thread. `Initialization` must be observed to handle errors; there is no built-in loading/error UI yet. This API is not thread-safe for simultaneous disposal from multiple threads and does not yet implement a dirty-close veto, operation supervision, or cancellation timeout. Activation failure cleanup is best effort. Do not ship this experimental API to NuGet yet.
