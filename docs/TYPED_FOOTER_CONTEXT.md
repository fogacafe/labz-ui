# Typed user and tenant context

Use `LabzUserContext` and `LabzTenantContext` to display an identity with
a stable ID, display name, and optional metadata without coupling the UI to
authentication or tenant management.

```csharp
shell.StatusBar.UserContext = new LabzUserContext
{
    Id = user.Id,
    DisplayName = user.Name,
    Email = user.Email
};

shell.StatusBar.TenantContext = new LabzTenantContext
{
    Id = tenant.Id,
    DisplayName = tenant.Name
};

shell.StatusBar.TenantContextRequested += (_, args) =>
{
    // args.Context?.Id identifies the current tenant.
    // The application owns selection, permissions, and any switch.
    ShowTenantSelector(args.Context?.Id);
};
```

Both records accept `Metadata` as `IReadOnlyDictionary<string,string>` for
additional display information. Avoid placing secrets or access tokens in UI
metadata.

The original `UserName`, `TenantName`, `UserRequested`, and
`TenantRequested` APIs remain supported. Setting a legacy name clears
the corresponding typed context; setting a typed context updates the name.
Both the contextual and legacy click events are raised for compatibility.
