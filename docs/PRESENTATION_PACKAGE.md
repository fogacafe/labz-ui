# Presentation package: typed navigation and cross-screen events

The Presentation layer supports scoped per-screen state, deferred initialization, guarded close, an application-wide event bus, and typed navigation parameters.

## Typed navigation

```csharp
public record OpenPositions(string Portfolio);

public sealed class PositionsPresenter :
    IAsyncScreenPresenter<PositionsForm>, IScreenParameters<OpenPositions>
{
    public void SetParameters(OpenPositions request) { /* capture request */ }
    // Attach, InitializeAsync, CloseAsync ...
}

workspace.Open<PositionsForm, PositionsPresenter, OpenPositions>(
    "Positions", new OpenPositions("Main"), ScreenOpenPolicy.ReuseByKey, "positions:main");
```

`SetParameters` runs after `Attach` but **before** `InitializeAsync`. A reused keyed screen keeps its original input; changing parameters requires closing/reopening or an explicit screen update mechanism. Use a key that reflects the identity of the input.

Register `AddLabzApplicationEventBus()` for cross-tab notifications; keep `IScreenEventBus` scoped for local messages. Dispose global subscriptions in `CloseAsync`. See [APPLICATION_EVENT_BUS.md](APPLICATION_EVENT_BUS.md).

Workspace shutdown now attempts to dispose every tab even if one presenter throws in `CloseAsync`, and propagates failures after cleanup. Guarded shutdown still checks close approval before disposal.

## Verification

Run Presentation tests on Windows and use [PRESENTATION_WINDOWS_QA.md](PRESENTATION_WINDOWS_QA.md) for manual checks. The sample Positions tab now receives a typed portfolio request. This package does not publish the experimental Presentation project to NuGet.
