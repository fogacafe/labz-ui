# Trading Desk end-to-end Showcase

Run the `Labz.UI.Showcase` Windows project, then click
**Open Trading Desk Demo** in the component gallery.

The full-window demo exercises:
- VS Code-style module activity bar and grouped sidebar navigation
- Persistent workspace tabs, Ctrl+Tab, Ctrl+Shift+Tab and Ctrl+W
- Ctrl+K command palette across modules
- Trade blotter, positions and settlements using LabzDataGrid
- Order entry and import pages with themed inputs
- User, environment and tenant context in the status footer
- Async tenant switching with a short simulated delay
- Workspace reset after tenant change, with unsaved-order confirmation
- Live theme changes inherited from the gallery

## Windows manual QA
1. Open several pages, switch modules and verify open tabs persist.
2. Use Ctrl+K and keyboard tab shortcuts.
3. Open Order Entry, type a draft and try switching tenant. Reject the
   close confirmation and verify the old tenant and tabs remain.
4. Approve the switch and verify old pages are disposed.
5. Toggle dark/light theme in the gallery and reopen the demo.
6. Inspect at 100%, 150% and 200% Windows DPI and narrow window sizes.

This is an interactive demonstration with in-memory sample data.
It does not implement real authentication, trading or persistence.
