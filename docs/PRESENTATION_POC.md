# Presentation scope proof of concept

Run on Windows:

```powershell
dotnet run --project samples/Labz.UI.PresentationDemo
```

Two editor Forms open as independent tabs. Each navigation call creates a new DI scope, Form, presenter and scoped state. Edit notes in each tab, open another and close tabs to exercise ownership. The Form is hosted borderlessly with `TopLevel = false`.

This is **not** the final navigation framework. Deliberately excluded: permissions, async lifecycle, event buses, loading, dirty close, modal dialogs and LabzTabWorkspace integration. The sample uses native TabControl as a minimal host so the experiment isolates DI and Form hosting.

## Limitations to resolve before a stable API

- Scope owns the view and presenter; avoid registering the same Form as a scoped dependency elsewhere, or double-disposal ownership can become ambiguous.
- A failed `Attach` must release any already-created Form and event subscriptions. Current factory disposes the scope; explicit transient Form cleanup requires a refined activation contract.
- A real host must support cancellable close and handle FormClosed/host shutdown coherently.
- Constructor cycles between Form and Presenter are intentionally avoided by explicit `Attach` after both resolve.
- All WinForms creation and mutation must run on the UI thread.
- Do not publish this experimental package as part of the NuGet release yet.
