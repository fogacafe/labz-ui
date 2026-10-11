using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware tooltip component with WinForms ToolTip behavior.</summary>
[ProvideProperty("ToolTip", typeof(Control))]
public class LabzTooltip : ToolTip
{
    public LabzTooltip()
    {
        OwnerDraw = true;
        Draw += DrawTooltip;
        Popup += OnPopup;
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
}
