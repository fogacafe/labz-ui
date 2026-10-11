# Presentation integration in Showcase

Run `dotnet run --project samples/Labz.UI.Showcase/Labz.UI.Showcase.csproj` on Windows and select **Open Presentation Demo**.

The new entry launches the existing PresentationDemo with its own dependency injection container, disposed when the demo window closes. Its tabs illustrate independently scoped views/presenters, a screen-local event bus, and a deferred loading window. Select **Deferred loading demo** to exercise typed navigation, reuse-by-key, asynchronous initialization, cancellation, close approval for unsaved notes, and application-wide events (publish a trade save and observe Positions).

The component gallery remains focused on visual controls; the PresentationDemo remains an experimental architecture sample, not a shipped Presentation NuGet package. Manual QA: open/close demo repeatedly, open multiple trade tabs, close while loading, open keyed trade twice, open positions and publish saved trade, toggle theme, then close all windows and check disposal. CI is required before merge.
