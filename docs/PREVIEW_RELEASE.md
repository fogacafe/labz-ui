# Labz UI preview release checklist

## Release candidate

Current CI builds and packs four packages at `0.1.0-preview.1`:
- Labz.UI.Core
- Labz.UI.Rendering
- Labz.UI.Validation
- Labz.UI.WinForms

The DataGrid v1 feature scope is frozen. The Showcase includes a dedicated
DataGrid demo with search, editing, keyed validation, local grouping and layout profiles.

## Required manual Windows QA

1. Run `dotnet run --project samples/Labz.UI.Showcase/Labz.UI.Showcase.csproj`.
2. Open **DataGrid v1** from the component gallery.
3. Toggle light/dark theme and inspect header, row and group-header contrast.
4. Edit Quantity with invalid and valid values; verify validation marker, tooltip and correction.
5. Edit Counterparty; verify grouping updates and group counts.
6. Search for an existing ticket and a missing ticket.
7. Collapse and expand each group; verify visible rows and keyboard navigation.
8. Right-click a column header; hide/show columns and drag to reorder.
9. Save a layout, change it, then load the saved layout.
10. Check window resizing, focus, high-DPI scaling and keyboard accessibility.

## Automated gates

- Windows build and tests succeed.
- Four `.nupkg` files are generated.
- Independent WinForms consumer restores from the local package feed and builds.

## Publishing sequence

1. Review package IDs and ownership on nuget.org (name availability is not guaranteed).
2. Choose the preview version; do not reuse an already published version.
3. Confirm README, license, project URL and transitive dependencies.
4. Download the `labz-ui-nuget` workflow artifact and inspect package contents.
5. Approve publishing explicitly.
6. Publish packages in dependency order: Core, Rendering, Validation, WinForms.
7. Verify NuGet indexing and install `Labz.UI.WinForms` in a clean project.

No credentials or publishing workflow are included. Do not publish without
explicit release approval.
