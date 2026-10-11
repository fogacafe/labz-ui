using System.ComponentModel;
using System.Drawing.Drawing2D;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>A designer-compatible tab control with themed headers and native keyboard navigation.</summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
public class LabzTabs : TabControl
{
    public LabzTabs()
    {
        DrawMode = TabDrawMode.OwnerDrawFixed;
        SizeMode = TabSizeMode.Normal;
        ItemSize = new Size(120, 38);
        Padding = new Point(18, 7);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(UiThemeManager.Current.Background);
        for (var i = 0; i < TabCount; i++)
            PaintTab(e.Graphics, i, i == SelectedIndex);
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        PaintTab(e.Graphics, e.Index, e.Index == SelectedIndex);
    }

    private void PaintTab(Graphics graphics, int index, bool selected)
    {
        if (index < 0 || index >= TabPages.Count) return;
        var theme = UiThemeManager.Current;
        var rect = GetTabRect(index);
        using var background = new SolidBrush(selected ? theme.Surface : theme.Background);
        graphics.FillRectangle(background, rect);
        var page = TabPages[index];
        var foreground = page.Enabled ? (selected ? theme.Primary : theme.Foreground) : SystemColors.GrayText;
        TextRenderer.DrawText(graphics, page.Text, Font, rect, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        if (selected)
        {
            using var underline = new SolidBrush(theme.Primary);
            graphics.FillRectangle(underline, rect.Left + 6, rect.Bottom - 3, Math.Max(1, rect.Width - 12), 3);
        }
        if (selected && Focused)
        {
            var focus = Rectangle.Inflate(rect, -5, -5);
            ControlPaint.DrawFocusRectangle(graphics, focus, foreground, theme.Surface);
        }
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is TabPage page)
        {
            page.BackColor = UiThemeManager.Current.Surface;
            page.ForeColor = UiThemeManager.Current.Foreground;
        }
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Background;
        ForeColor = theme.Foreground;
        foreach (TabPage page in TabPages)
        {
            page.BackColor = theme.Surface;
            page.ForeColor = theme.Foreground;
        }
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
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
