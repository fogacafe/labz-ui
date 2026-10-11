# Presentation — Windows manual QA checklist

Use the `Labz.UI.PresentationDemo` sample on a Windows desktop with .NET 10 installed. Automated STA tests cover important lifecycle paths, but do not replace interactive checks.

## Opening and navigation
- [ ] Open two independent Trade tabs; verify independent Notes and loading state.
- [ ] Open the same keyed Trade tab twice; verify the existing tab is selected rather than duplicated.
- [ ] Close a keyed tab and reopen; verify a fresh instance appears.
- [ ] Open Positions and Trade tabs together; verify heterogeneous tabs behave independently.

## Loading and cancellation
- [ ] Open a Trade tab; verify the overlay/spinner appears and eventually clears.
- [ ] Close a Trade tab while loading; verify the window remains responsive and no disposed-control errors occur.
- [ ] Close one loading tab while another loads; verify the second continues independently.
- [ ] Switch tabs while loading; verify indicator text and state remain scoped.

## Dirty close guard
- [ ] Edit Trade Notes, close the tab and choose Cancel; verify tab and edits remain.
- [ ] Repeat and choose No (discard); verify tab closes.
- [ ] Repeat and choose Yes (demo fake-save); verify tab closes.
- [ ] With multiple dirty Trade tabs, close the window; reject approval on a later tab and verify **all** tabs remain open.
- [ ] Approve all tabs; verify window exits normally without a repeated confirmation loop.
- [ ] Try closing the window while a tab is loading; verify shutdown and cancellation behave consistently.

## Error paths
- [ ] Verify initialization errors can be surfaced to the user without leaving a permanent loading overlay.
- [ ] Verify errors in close callbacks are shown and resources are cleaned up (may require a deliberate fault-injection sample).
- [ ] Verify repeated close clicks do not duplicate cleanup or create orphaned tabs.

## Before release
- [ ] Run Windows CI tests and confirm green.
- [ ] Manually check light and dark loading overlay palettes.
- [ ] Confirm no unobserved task exceptions in debugger output.
- [ ] Record Windows version, .NET runtime version, result, and any screenshots/issues.

**Known limits:** Notes nonempty is only a demo dirty heuristic; Yes does not persist data. Approval callbacks may save before a later tab vetoes shutdown; those side effects are not rolled back. Forced `DisposeAsync` bypasses guards. Some error scenarios require fault injection and cannot be exercised in the unmodified demo.
