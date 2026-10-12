# Labz UI visual identity — first foundation

## Direction

A modern, dense trading desktop: JetBrains-like restraint with financial-terminal information density. Flat is acceptable; undifferentiated surfaces are not.

## Theme contract

The existing seven positional `UiTheme` fields remain unchanged to preserve compatibility. The new read-only semantic tokens provide a shared vocabulary:

| Token | Intended use |
| --- | --- |
| `Background` | App canvas |
| `Surface` | Primary work area |
| `SidebarSurface` | Module/navigation rail |
| `ElevatedSurface` | Floating or raised panels |
| `SubtleBorder` | Quiet section boundaries |
| `Border` | Interactive-control outline |
| `MutedForeground` | Secondary metadata |
| `SelectionSurface` | Selected navigation/rows |
| `Primary` | Active action/focus accent |

The dark palette now has a cooler slate foundation and a more restrained blue accent. All tokens are derived from theme mode; no per-form hardcoded color constants should be introduced.

## Next implementation slices

1. Wire semantic tokens into `LabzAppShell` sidebar, header and status bar.
2. Refine `LabzTabWorkspace` active/inactive tabs and spacing.
3. Introduce a page header/toolbar composition in Trading Desk Demo.
4. Refine `LabzDataGrid` header, row density, hover and selection without sacrificing legibility.

## Validation

- Run `dotnet build Labz.UI.slnx` and `dotnet test Labz.UI.slnx` on Windows/.NET 10.
- Inspect light and dark themes, 100/150/200% DPI, keyboard focus, high-contrast/readability and Designer rendering.
- The current PR is **only the token/palette foundation**, not the completed shell or grid restyle.
