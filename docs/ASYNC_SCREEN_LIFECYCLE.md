# Experimental asynchronous screen lifecycle

`IAsyncScreenPresenter<TView>` extends `IScreenPresenter<TView>` with `InitializeAsync(CancellationToken)` and `CloseAsync(CancellationToken)`.

`AsyncScreenFactory.OpenAsync<TView, TPresenter>` creates a fresh DI scope, attaches the presenter, and awaits initialization. An exception during initialization disposes the scope and instance. `AsyncScreenInstance<TView>.DisposeAsync()` cancels its lifetime token, calls `CloseAsync`, disposes the Form, then disposes its async DI scope. Disposal is idempotent.

**Important limitations (POC):**

- This is an opt-in API alongside the synchronous factory; the existing sample is unchanged.
- Invoke it from the UI thread. Presenter code that awaits without returning to the UI thread must explicitly marshal before touching controls.
- `OpenAsync` currently awaits initialization **before returning the Form**. The host cannot display a loading shell during initialization without a later two-phase API.
- The lifetime token is exposed for operations started by the presenter after initialization, but the presenter is not yet given it automatically.
- `CloseAsync` is a cleanup hook, not a cancellable dirty-close guard; it receives `CancellationToken.None` so cleanup can complete.
- The implementation does not wait for arbitrary presenter-started background tasks before disposal. Presenters must stop and await their own operations during `CloseAsync`.
- Concurrent close and open, host shutdown, UI-thread affinity, and partial activation failure require further tests and design.
- No NuGet publication for this experimental API.
