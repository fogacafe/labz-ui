# Visual Polish — Navigation and surfaces

This package targets issues reported in Windows Showcase screenshots: the native white TabControl header strip and clipped Summary / No Results content.

Changes:
- LabzTabs paints its own themed header background and tab items, retaining native tab navigation and TabPage hosting.
- LabzEmptyState uses more compact heading and description regions.
- Showcase Summary uses docked content rather than a fixed coordinate, and gives both examples more vertical space.

Windows acceptance: inspect selected and inactive tabs in light/dark, Ctrl+Tab and keyboard focus, resizing, scaling at 100/125/150%, Summary and No Results text visibility. Native WinForms tab-page borders may still require targeted work after visual QA. CI validates compilation, not pixels.
