# Labz UI

A modern, themeable, open-source component library for Windows Forms.

> Early bootstrap: APIs and visual styling are experimental and not yet production-ready.

## Goals

- Own the visual design while preserving native Windows Forms behavior where useful.
- Support the Visual Studio Designer and programmatic construction.
- Provide customizable light/dark themes and reusable design tokens.
- Offer first-class field validation and accessible interaction states.

## Requirements

- Windows
- .NET 10 SDK
- Visual Studio with Windows Forms tooling (recommended for Designer)

## Try the showcase

```powershell
dotnet restore Labz.UI.slnx
dotnet build Labz.UI.slnx
dotnet test Labz.UI.slnx
dotnet run --project samples/Labz.UI.Showcase
```

## Example

```csharp
using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;

UiThemeManager.Apply(UiTheme.Dark);

var button = new LabzButton { Text = "Save", Outline = false };
```

## Projects

- `Labz.UI.Core`: theme contracts
- `Labz.UI.Rendering`: drawing primitives
- `Labz.UI.WinForms`: Windows Forms controls
- `Labz.UI.Validation`: UI-agnostic validation results
- `Labz.UI.Showcase`: sample application

Read [architecture](docs/ARCHITECTURE.md) and [contributing](CONTRIBUTING.md) to get started.

## License

MIT.
