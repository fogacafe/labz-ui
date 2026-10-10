# Architecture

Labz UI is a modern, themeable Windows Forms component library.

- **Core**: immutable theme tokens and shared contracts.
- **Rendering**: Windows-specific drawing primitives.
- **WinForms**: designer-compatible controls and event handling.
- **Validation**: UI-independent validation result contracts.
- **Showcase**: a runnable gallery for component states.

## Principles

1. Own the visuals, reuse reliable native editing behaviors where appropriate.
2. Support both the Visual Studio Designer and fluent programmatic construction.
3. Keep validation rules separate from visual feedback.
4. Support keyboard accessibility, focus states, high DPI, and predictable disposal.
5. Treat the initial button as a proof of concept, not a finished accessibility or design-system implementation.

## Roadmap

- Theme tokens: hover, pressed, disabled, focus, spacing, typography.
- Button variants and designer verification.
- TextBox with native text editing and custom chrome.
- Field validation adapter, async rules, error messages.
- ComboBox, DatePicker, and eventually DataGrid.
