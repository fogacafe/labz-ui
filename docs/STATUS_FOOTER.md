# Status footer: user, tenant and environment

`LabzStatusBar` is a standalone theme-aware footer, included by default in
`LabzAppShell` via `shell.StatusBar`.

```csharp
shell.StatusBar.StatusText = "Connected";
shell.StatusBar.EnvironmentName = "Production";
shell.StatusBar.UserName = currentUser.DisplayName;
shell.StatusBar.TenantName = currentTenant.DisplayName;

shell.StatusBar.UserRequested += (_, _) => ShowProfile();
shell.StatusBar.TenantRequested += (_, _) => ShowTenantSelector();
```

The host application owns user identity, tenant selection, authentication and
authorization. The library **does not** fetch users, switch tenants, persist
credentials or change the active context automatically.

All display properties are optional. Missing user, tenant and environment
values hide their respective footer items. Click events only fire when the
corresponding context button is visible and clicked.

A standalone footer can be used without the AppShell:

```csharp
form.Controls.Add(new LabzStatusBar { Dock = DockStyle.Bottom });
```
