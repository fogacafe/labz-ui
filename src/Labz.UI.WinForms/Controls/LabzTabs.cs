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
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [Category("Data")]
    public new TabPageCollection TabPages => base.TabPages;

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= TabPages.Count) return;
        var theme = UiThemeManager.Current;
        var rect = GetTabRect(e.Index);
        var selected = e.Index == SelectedIndex;
        using var background = new SolidBrush(selected ? theme.Surface : theme.Background);
        e.Graphics.FillRectangle(background, rect);
        var page = TabPages[e.Index];
        var foreground = page.Enabled ? (selected ? theme.Primary : theme.Foreground) : SystemColors.GrayText;
        TextRenderer.DrawText(e.Graphics, page.Text, Font, rect, foreground,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
        if (selected)
        {
            using var underline = new SolidBrush(theme.Primary);
            e.Graphics.FillRectangle(underline, rect.Left + 6, rect.Bottom - 3, Math.Max(1, rect.Width - 12), 3);
        }
        if ((e.State & DrawItemState.Focus) != 0 && Focused)
        {
            var focus = Rectangle.Inflate(rect, -5, -5);
            ControlPaint.DrawFocusRectangle(e.Graphics, focus, foreground, theme.Surface);
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
