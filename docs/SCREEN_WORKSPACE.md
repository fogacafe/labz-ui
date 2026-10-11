# ScreenWorkspace MVP

`ScreenWorkspace` hosts different `Form` / `IAsyncScreenPresenter<TView>` pairs in one `TabControl`. It delegates screen creation and `ReuseByKey` to `ScreenNavigator`, attaches each Form to a tab, selects reused tabs, and asynchronously closes/disposes each screen and its DI scope.

```csharp
var workspace = new ScreenWorkspace(navigator, tabs);
workspace.Open<TradeForm, TradePresenter>("Trade 123", ScreenOpenPolicy.ReuseByKey, "trade:123");
workspace.Open<PositionForm, PositionPresenter>("Positions");
await workspace.CloseSelectedAsync();
await workspace.DisposeAsync();
```

The snippet assumes both presenters implement `IAsyncScreenPresenter<TView>`. This is a UI-thread-only MVP. Host Form must await workspace disposal during shutdown. It does not yet own navigation permissions, dirty-close guards, loading/error status, or automatically hook FormClosing. Avoid using the same `ScreenNavigator` across multiple workspaces: keyed lookup is navigator-wide.
