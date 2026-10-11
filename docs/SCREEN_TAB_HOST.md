# ScreenTabHost MVP

`ScreenTabHost<TView,TPresenter>` bridges `ScreenNavigator` to an existing WinForms `TabControl`. `Open(title, policy, key)` attaches the Form, selects its tab, and re-selects a keyed instance when it already exists. `CloseSelectedAsync`/`CloseAsync` remove the tab, call `Forget`, cancel/close the presenter and dispose the screen scope; `DisposeAsync` closes remaining screens.

```csharp
var host = new ScreenTabHost<TradeForm, DeferredTradePresenter>(navigator, tabs);
host.Open("Trade 123", ScreenOpenPolicy.ReuseByKey, "trade:123");
await host.CloseSelectedAsync();
await host.DisposeAsync();
```

Use only on the WinForms UI thread. The application must await disposal during its shutdown. This MVP hosts one view/presenter pair per host; multi-type tabs, dirty-close guards, automatic host FormClosing integration and initialization status UI are future work. Visual QA on Windows is required.
