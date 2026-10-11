# RFC 001: Labz UI Presentation Architecture

**Status:** Proposed — discussion only; no runtime implementation in this PR.

## Goals

Provide an optional, testable WinForms application architecture around existing Labz UI controls: MVP, scoped navigation, authorization, intra-screen events, loading and lifecycle management. Preserve use of Labz.UI.WinForms without the presentation framework.

## Package boundaries (proposed)

- **Labz.UI.Presentation**: UI-agnostic contracts, screen descriptors, presenter lifecycle, navigation abstractions, typed event bus, loading operation contracts and authorization results. Avoid WinForms dependencies where possible.
- **Labz.UI.Hosting**: Microsoft.Extensions.DependencyInjection integration, screen scope ownership, presenter activation, navigation composition, WinForms hosting adapters and themed authorization dialogs.
- **Labz.UI.WinForms**: existing visual controls; must not depend on Presentation or Hosting.
- **Application**: business use cases, backend authorization and persistence. No service locator in presenters.

Names and project boundaries are subject to proof-of-concept validation.

## Screen registration and navigation

Conceptual API (not implemented):

```csharp
services.AddLabzPresentation(options =>
{
    options.AddScreen<ITradeListView, TradeListForm, TradeListPresenter>(screen =>
    {
        screen.Title = "Trades";
        screen.Permission = "TRADES_VIEW";
        screen.OpenMode = ScreenOpenMode.Tab;
        screen.InstancePolicy = ScreenInstancePolicy.NewInstance;
    });
});

await navigator.OpenAsync<ITradeEditView>(
    new TradeEditArgs(tradeId: 1024),
    cancellationToken);
```

Registration maps the **view interface** to a concrete Form and presenter. Navigation resolves by interface, never by concrete Form. The default instance policy is **NewInstance**. Optional **ReuseByKey** and **Singleton** are explicit per-screen policies. Screen identity (tab instance) is distinct from domain identity (trade ID). Two editor tabs for the same trade may coexist; optimistic concurrency belongs to the application/backend.

## Navigation sequence and ownership

1. Find registered screen descriptor; reject unregistered screens.
2. Evaluate navigation permission **before constructing the view or screen scope**.
3. If denied, display a themed dialog explaining the exact permission to request; do not open the tab. The application can choose whether unauthorized menu entries remain visible.
4. Apply the instance policy. Reusing an existing tab must not construct another presenter or scope. Reauthorization policy for reuse needs explicit definition.
5. Create a child IServiceScope for a new screen.
6. Resolve the view Form, presenter and scoped screen services through constructor injection; avoid circular constructor references and static service resolution. Define a composition/activation strategy in the implementation RFC.
7. Initialize presenter asynchronously once, then host the Form in a tab (e.g. TopLevel=false, borderless, Dock=Fill) via a dedicated host adapter.
8. On close request, run a cancellable dirty-state/close guard. If approved, cancel in-flight operations, unsubscribe handlers, close/dispose hosted Form, dispose async-capable services and scope exactly once.
9. Activation/deactivation of tabs is separate from creation/destruction.

A hosted Form is not a top-level modal window. Distinguish tabbed `OpenAsync` from modal `ShowDialogAsync`; modal ownership, result and WinForms threading must be specified and tested. Do not promise native modal semantics for a Form embedded in a tab.

## MVP contracts

- View interfaces expose data, UI events and presentation-specific operations without leaking concrete controls.
- Presenter depends on the view interface and application services via constructor injection.
- Consider `IView`, `IWindowView` and `IPresenter<TView>` with minimal lifecycle methods; avoid a giant base class.
- Resolve ambiguity around `Show`, `ShowDialog` and `Close`: preferably navigation/window services mediate display while `IView` stays small. Existing application ergonomics are a design input.
- Support reusable UserControls with their own presenters in the same screen scope. Child components should not depend directly on sibling controls.
- Test presenters without constructing WinForms controls.

## Event buses

- **IScreenEventBus** is scoped to one tab/Form instance and shared with its child UserControls. Typed publish/subscribe; subscriptions return `IDisposable`.
- **IApplicationEventBus** is explicitly application-scoped for cross-tab domain notifications (e.g. `TradeSaved`). No automatic forwarding of local events.
- Specify sync vs async publish, exception policy, reentrancy, ordering and UI SynchronizationContext marshaling. Do not use async-void subscribers.
- Screen closure disposes all screen subscriptions and prevents delivery into disposed controls.

## Loading manager

- A screen-scoped `ILoadingManager.Begin(message, timeout?)` returns an operation token implementing `IDisposable` (potentially `IAsyncDisposable`).
- Each operation has a unique identity; multiple concurrent operations remain visible until all finish. A dictionary/count of active operations is safer than LIFO stack assumptions.
- Timeout expires only its own token, is observable/logged, and **does not imply the underlying operation was cancelled**. Cancellation requires a separate CancellationToken policy.
- Define which message wins when operations overlap and how to marshal state changes onto the UI thread.
- Tokens are idempotently disposed and cleaned up when the screen scope closes.

## Authorization

- Application-supplied `IScreenAuthorizationService` returns allow/deny and required permission information.
- Configurable unauthorized-menu visibility; denial dialog shows requested permission and optionally a copyable access request.
- Navigation checks are UX controls, **not** a security boundary: APIs must enforce authorization independently.
- Define handling of transient permission service failures and changes to permission while a screen is open.

## Test and rollout strategy

1. RFC review and architecture decisions; no implementation in this PR.
2. Minimal MVP + scoped DI + one Form-per-tab proof of concept, verifying multiple independent instances.
3. Typed screen bus and disposable subscriptions; test isolation and cleanup.
4. Loading tokens with concurrent operations, timeout and UI dispatch tests.
5. Authorization integration and denied-navigation dialog, including failure cases.
6. Cross-tab events and modal hosting only after lifecycle behavior is stable.
7. Showcase: list trades, open two edit tabs, compare and save; demonstrate child UserControls, permissions, loading and dirty close.

## Open decisions

- Presenter/view activation without constructor cycles: factory, explicit attach, or an activation contract?
- How to pass typed navigation arguments to presenters and avoid implicit mutable state?
- What is the policy for failed initialization and partial scope cleanup?
- How should reauthorization work for reused tabs?
- What is the exact modal API for standalone Forms versus tab-hosted Forms?
- How should the host handle UI thread dispatch and shutdown while operations are pending?
- What is the API for explicit opt-in cross-tab event delivery and cleanup?
