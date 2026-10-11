# ScreenNavigator MVP

`ScreenNavigator` is a UI-thread-only registry around `DeferredScreenFactory`. Use `NewInstance` (default) for independent Form/scope creation, or `ReuseByKey` to return the existing live Form for a `(view type, key)` pair. `ReuseByKey` requires a nonblank key. Each navigator should be scoped to one tab host. The caller remains responsible for selecting/attaching the returned view, observing `Initialization`, closing the tab, awaiting `DisposeAsync()`, and calling `Forget(instance)` when removed.

```csharp
var first = navigator.Open<TradeForm, DeferredTradePresenter>(ScreenOpenPolicy.ReuseByKey, "trade:123");
var again = navigator.Open<TradeForm, DeferredTradePresenter>(ScreenOpenPolicy.ReuseByKey, "trade:123");
// ReferenceEquals(first, again) == true
```

This initial API does **not** automatically own tabs, navigation history, permissions or dirty-close prompts. It is not thread-safe. Host-managed cleanup is intentional until the navigation lifecycle contract is agreed.
