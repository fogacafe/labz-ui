# Screen Loading Manager — initial contract

Register `IScreenLoadingManager` as scoped. Each Form/tab scope gets its own manager. Operations receive unique tokens; completion order need not match start order.

```csharp
using var loading = manager.Begin("Loading trades");
await tradeService.LoadAsync(cancellationToken);
```

`IsLoading` stays true while any token is active. `ActiveCount` reports the number of operations. `Message` is the most recently **started still-active** operation's message. `Changed` fires when operations begin or end.

**Limitations:** no timeout or automatic expiration yet, no built-in spinner binding, no UI SynchronizationContext dispatch, and no async disposal contract. `Changed` is raised on the calling thread; UI consumers must marshal before updating WinForms controls. Tokens only track UI state and do not cancel work. This is a small primitive, not a full task supervisor. Do not publish the experimental package yet.
