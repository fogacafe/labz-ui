# Scoped screen event bus (initial synchronous contract)

`IScreenEventBus` is intended for a single Form/tab scope. Child UserControls and their presenters can publish and subscribe to typed events without referencing sibling controls.

```csharp
public sealed record TradeSelected(int TradeId);

using var subscription = bus.Subscribe<TradeSelected>(e => ShowTrade(e.TradeId));
bus.Publish(new TradeSelected(1024));
```

Register `IScreenEventBus` as scoped and resolve it from the same screen scope. The current `ScreenFactory` does not yet register the event bus automatically; the hosting integration is a subsequent step.

- Subscriptions return an idempotent `IDisposable`.
- Publication is synchronous and invokes subscribers in subscription order on the calling thread.
- A snapshot is used for dispatch: unsubscribing during a publication affects future publications, not the current snapshot.
- Exceptions propagate to the publisher and stop subsequent delivery. Do not use `async void` handlers.
- UI subscribers must marshal to the WinForms UI thread when publishing from background work.
- Publishing or subscribing after bus disposal throws `ObjectDisposedException`.
- Cross-tab events are deliberately **not** supported by this interface. An explicit application bus will be considered separately.
