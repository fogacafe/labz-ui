# Application event bus (cross-screen)

Use `services.AddLabzApplicationEventBus()` to register one `IApplicationEventBus` singleton shared by all screen scopes.

Keep `services.AddScoped<IScreenEventBus, ScreenEventBus>()` for **screen-local** communication. The two buses are intentionally separate.

```csharp
// Publisher in one screen presenter
_appEvents.Publish(new TradeSaved(tradeId));

// Subscriber in another screen presenter
_subscription = _appEvents.Subscribe<TradeSaved>(e => RefreshTrade(e.TradeId));
// Dispose _subscription when the subscriber's screen closes.
```

Use an application-owned event contract (e.g. `record TradeSaved(int TradeId)`).
Callbacks run synchronously on the publishing thread and exceptions propagate to the publisher. This is **in-process notification**, not a durable message queue. UI subscribers must marshal to the WinForms UI thread when publishing off-thread. Dispose subscription tokens when a screen closes, otherwise singleton subscriptions retain their handlers and potentially their views. No global navigation or replay is implied.
