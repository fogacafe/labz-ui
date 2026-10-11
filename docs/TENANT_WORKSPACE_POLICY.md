# Tenant-aware workspace lifecycle

Set `shell.ResetWorkspaceOnTenantChange = true` to prevent pages from one
tenant being reused after switching to another.

```csharp
shell.ConnectWorkspace(workspace, CreatePage);
shell.ResetWorkspaceOnTenantChange = true;
shell.StatusBar.TenantSwitchHandler = async (_, requested, ct) =>
    await session.SwitchAsync(requested.Id, ct);
```

Before a tenant change, the shell asks every open page's `PageClosing`
handler whether closing is allowed. Any veto cancels the tenant switch.
After the tenant is successfully committed, all old tabs are disposed.

For custom workflows, `workspace.CanCloseAll()` checks dirty-state guards
without closing tabs, and `workspace.CloseAllAfterApproval()` disposes
pages after a host-owned confirmation.

The policy is opt-in to preserve existing behavior. For multitenant
applications, enable it or implement equivalent tenant-scoped page isolation.
Host applications remain responsible for ensuring their asynchronous session
switch is atomic and that no old-tenant requests update the UI afterward.
