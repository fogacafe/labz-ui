namespace Labz.UI.WinForms.Dialogs;

/// <summary>Convenience API for theme-aware modal messages and confirmations.</summary>
public static class LabzMessageBox
{
    public static DialogResult Show(string message, string title = "Message",
        LabzDialogKind kind = LabzDialogKind.Information,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        IWin32Window? owner = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(title);
        using var dialog = new LabzDialog(message, title, kind, buttons);
        return owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
    }

    public static DialogResult Confirm(string message, string title = "Confirm", IWin32Window? owner = null)
        => Show(message, title, LabzDialogKind.Question, MessageBoxButtons.YesNo, owner);
}
