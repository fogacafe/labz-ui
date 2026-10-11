using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;

namespace Labz.UI.WinForms.Dialogs;

public enum LabzDialogKind { Information, Success, Warning, Error, Question }

/// <summary>Theme-aware modal dialog using native WinForms dialog semantics.</summary>
public sealed class LabzDialog : Form
{
    private readonly Label _title = new() { AutoSize = true, Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 13, FontStyle.Bold) };
    private readonly Label _message = new() { AutoSize = false, Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _actions = new()
    {
        Dock = DockStyle.Bottom, Height = 62, FlowDirection = FlowDirection.RightToLeft,
        WrapContents = false, Padding = new Padding(16, 10, 16, 8)
    };
    private readonly Panel _header = new() { Dock = DockStyle.Top, Height = 64, Padding = new Padding(24, 20, 24, 8) };
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new Padding(24, 12, 24, 10) };
    private readonly LabzDialogKind _kind;

    public LabzDialog(string message, string title, LabzDialogKind kind = LabzDialogKind.Information,
        MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        _kind = kind;
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 220);
        MinimumSize = new Size(350, 220);
        Font = SystemFonts.MessageBoxFont;
        _title.Text = title;
        _title.MaximumSize = new Size(390, 0);
        _header.Controls.Add(_title);
        _message.Text = message;
        _message.AutoEllipsis = true;
        _message.AccessibleName = "Dialog message";
        _content.Controls.Add(_message);
        Controls.AddRange([_content, _actions, _header]);
        ConfigureButtons(buttons);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    private void ConfigureButtons(MessageBoxButtons buttons)
    {
        (string Caption, DialogResult Result, bool Primary)[] actions = buttons switch
        {
            MessageBoxButtons.OKCancel => [("Cancel", DialogResult.Cancel, false), ("OK", DialogResult.OK, true)],
            MessageBoxButtons.YesNo => [("No", DialogResult.No, false), ("Yes", DialogResult.Yes, true)],
            MessageBoxButtons.YesNoCancel => [("Cancel", DialogResult.Cancel, false),
                ("No", DialogResult.No, false), ("Yes", DialogResult.Yes, true)],
            MessageBoxButtons.RetryCancel => [("Cancel", DialogResult.Cancel, false), ("Retry", DialogResult.Retry, true)],
            MessageBoxButtons.AbortRetryIgnore => [("Ignore", DialogResult.Ignore, false),
                ("Retry", DialogResult.Retry, true), ("Abort", DialogResult.Abort, false)],
            _ => [("OK", DialogResult.OK, true)]
        };

        foreach (var action in actions)
        {
            var button = new LabzButton
            {
                Text = action.Caption, DialogResult = action.Result, Width = 100,
                Variant = action.Primary ? LabzButtonVariant.Solid : LabzButtonVariant.Outline,
                Margin = new Padding(5, 0, 0, 0)
            };
            _actions.Controls.Add(button);
            if (action.Primary) AcceptButton = button;
            if (action.Result == DialogResult.Cancel) CancelButton = button;
        }
        if (CancelButton is null && buttons == MessageBoxButtons.YesNo)
            ControlBox = false; // Prevent dismissing a binary choice without a result.
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        ForeColor = theme.Foreground;
        _header.BackColor = theme.Background;
        _content.BackColor = theme.Background;
        _actions.BackColor = theme.Background;
        _title.ForeColor = _kind switch
        {
            LabzDialogKind.Error => theme.Danger,
            LabzDialogKind.Warning => Color.DarkOrange,
            LabzDialogKind.Success => Color.ForestGreen,
            _ => theme.Foreground
        };
        _message.ForeColor = theme.Foreground;
        Invalidate(true);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke((Action)ApplyTheme);
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            UiThemeManager.ThemeChanged -= OnThemeChanged;
            _title.Font.Dispose();
        }
        base.Dispose(disposing);
    }
}
