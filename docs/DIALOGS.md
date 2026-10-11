# Theme-aware modal dialogs

`LabzMessageBox` replaces the stock WinForms `MessageBox` for application
messages and confirmations. It uses `LabzButton` and the active `UiTheme`
while preserving `DialogResult`, keyboard Enter/Escape, and owner-modal behavior.

```csharp
using Labz.UI.WinForms.Dialogs;

LabzMessageBox.Show("Saved!", "Success", LabzDialogKind.Success, owner: this);

if (LabzMessageBox.Confirm("Delete this item?", "Confirm deletion", this)
    == DialogResult.Yes)
{
    DeleteItem();
}

// For customization, construct LabzDialog directly:
using var dialog = new LabzDialog("Please review the details", "Review",
    LabzDialogKind.Warning, MessageBoxButtons.OKCancel);
var result = dialog.ShowDialog(this);
```

This initial version provides Information, Success, Warning, Error and Question
semantics and standard WinForms button combinations. A future PR can add custom
dialog content and non-modal toasts.
