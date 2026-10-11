# DataGrid v1 release gate

The DataGrid v1 scope is intentionally frozen after integration testing.

## Included
- Native binding, selection, editing, sorting and filtering delegation.
- Search navigation for locally available rows; provider search for virtual mode.
- Opt-in virtual rows and host-managed paging/cache.
- Column reordering, visibility selector and JSON layout profiles.
- Row/cell validation markers, including stable record-key mapping.
- Host-defined formatting, painting and change handlers.
- One-level local grouping and separate themed group header presenter.

## Explicit limitations
- Group headers are not interleaved into the native DataGridView rows.
- Grouping is local-only; use provider-side grouping for large virtual data.
- RefreshGrouping is needed after in-place group-key mutations.
- Positional validation markers must be rebuilt when row order changes.
- Keyed validation requires stable unique record IDs and column names.
- Search is a synchronous local scan, not an indexed search engine.

## Release acceptance
- Windows CI build, test and pack green.
- Integration tests for editing, searching, keyed validation, grouping, layout profiles.
- Manual Showcase QA: editing, checkbox commit, sort/filter, rebind, collapse/expand, theme switch, layout restore.
- Performance smoke tests on realistic local row counts and virtual-mode paging.
- Review public APIs and NuGet metadata; test installing the generated nupkg in a clean WinForms app.
- Publish only after manual approval; do not publish from this PR.
