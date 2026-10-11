using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware designer extender for native WinForms tooltips.</summary>
[ProvideProperty("LabzToolTip", typeof(Control))]
public class LabzTooltip : Component, IExtenderProvider
{
    private readonly ToolTip _tooltip = new() { OwnerDraw = true };

    public LabzTooltip()
    {
        _tooltip.Draw += DrawTooltip;
        _tooltip.Popup += OnPopup;
    }

    public LabzTooltip(IContainer container) : this() => container.Add(this);

    public bool CanExtend(object extendee) => extendee is Control;

    [DefaultValue("")]
    public string GetLabzToolTip(Control control) => _tooltip.GetToolTip(control);

    public void SetLabzToolTip(Control control, string text) => _tooltip.SetToolTip(control, text);

    public int AutoPopDelay
    {
        get => _tooltip.AutoPopDelay;
        set => _tooltip.AutoPopDelay = value;
    }

    public int InitialDelay
    {
        get => _tooltip.InitialDelay;
        set => _tooltip.InitialDelay = value;
    }

    private void OnPopup(object? sender, PopupEventArgs e)
    {
        var size = e.ToolTipSize;
        e.ToolTipSize = new Size(Math.Max(80, size.Width + 18), size.Height + 10);
    }

    private void DrawTooltip(object? sender, DrawToolTipEventArgs e)
    {
        var theme = UiThemeManager.Current;
        using var background = new SolidBrush(theme.Surface);
        using var border = new Pen(theme.Border);
        e.Graphics.FillRectangle(background, e.Bounds);
        e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, e.Bounds.Width - 1), Math.Max(0, e.Bounds.Height - 1));
        TextRenderer.DrawText(e.Graphics, e.ToolTipText, SystemFonts.MessageBoxFont,
            Rectangle.Inflate(e.Bounds, -9, -5), theme.Foreground,
            TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tooltip.Dispose();
        base.Dispose(disposing);
    }
}
