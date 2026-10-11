# Built-in multi-tenant selector

The status footer optionally displays a context menu containing the tenants
supplied by the host application. It never discovers tenants or grants access.

```csharp
shell.StatusBar.TenantContext = currentTenant;
shell.StatusBar.SetAvailableTenants(authorizedTenants);
shell.StatusBar.TenantSelectorEnabled = true;

shell.StatusBar.TenantChanging += (_, e) =>
{
    // Synchronous veto: check permissions and unsaved state.
    if (!CanSwitchTo(e.Requested.Id)) e.Cancel = true;
};
shell.StatusBar.TenantChanged += (_, e) => ReloadForTenant(e.Tenant.Id);
```

`TrySelectTenant(id)` supports programmatic selection. It returns false if
the tenant is unknown, already selected, or canceled by a handler.
A successful selection updates `TenantContext` and raises `TenantChanged`.
Tenant IDs must be unique (case-insensitive).

**Important:** This is a synchronous selection UI, not an authorization or
transaction boundary. For asynchronous tenant switching, validate and load
the new tenant context **before** calling `TrySelectTenant`, or veto the
automatic selection and manage the process in the host. Do not use
`TenantChanged` to initiate a potentially failing asynchronous switch while
assuming the UI remains on the previous tenant.

Existing `TenantRequested` and `TenantContextRequested` events still fire,
including when the built-in selector is enabled.
