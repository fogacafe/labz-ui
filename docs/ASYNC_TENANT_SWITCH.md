# Safe asynchronous tenant switching

Use `TenantSwitchHandler` when changing tenant requires an API request,
authorization, or loading new application context. The UI retains the old
tenant until the handler returns `true`. Concurrent switches are rejected,
and the selector is disabled while the operation is pending.

```csharp
shell.StatusBar.TenantSwitchHandler = async (current, requested, ct) =>
{
    // Host owns authorization, loading and committing application context.
    return await tenantSession.SwitchAsync(requested.Id, ct);
};

shell.StatusBar.TenantSwitchFailed += (_, e) =>
    ShowError(e.Exception.Message);
```

Clicking the built-in tenant menu calls `TrySelectTenantAsync`.
The legacy synchronous `TrySelectTenant` returns false while an async handler
is configured, avoiding bypass of host validation.

A handler returning false, throwing, or cancellation leaves the displayed
tenant unchanged. `TenantChanged` fires only on success. The host must
ensure its own switch is atomic: the UI cannot undo server-side mutations.

**Next milestone:** define what happens to open pages/tabs after a successful
tenant switch, so pages never retain data from the previous tenant.
