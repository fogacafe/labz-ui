using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Context menu that follows the Labz theme while retaining native menu navigation.</summary>
public class LabzContextMenu : ContextMenuStrip
{
    public LabzContextMenu()
    {
        Renderer = new ToolStripProfessionalRenderer(new LabzMenuColors());
        ShowImageMargin = true;
        UiThemeManager.ThemeChanged += OnThemeChanged;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        BackColor = theme.Surface;
        ForeColor = theme.Foreground;
        foreach (ToolStripItem item in Items)
        {
            item.BackColor = theme.Surface;
            item.ForeColor = theme.Foreground;
        }
        Invalidate(true);
    }

    protected override void OnItemAdded(ToolStripItemEventArgs e)
    {
        base.OnItemAdded(e);
        if (e.Item is not null)
        {
            e.Item.BackColor = UiThemeManager.Current.Surface;
            e.Item.ForeColor = UiThemeManager.Current.Foreground;
        }
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

    private sealed class LabzMenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => UiThemeManager.Current.Primary;
        public override Color MenuItemSelectedGradientBegin => UiThemeManager.Current.Primary;
        public override Color MenuItemSelectedGradientEnd => UiThemeManager.Current.Primary;
        public override Color MenuItemBorder => UiThemeManager.Current.Border;
        public override Color MenuBorder => UiThemeManager.Current.Border;
        public override Color ToolStripDropDownBackground => UiThemeManager.Current.Surface;
        public override Color ImageMarginGradientBegin => UiThemeManager.Current.Surface;
        public override Color ImageMarginGradientMiddle => UiThemeManager.Current.Surface;
        public override Color ImageMarginGradientEnd => UiThemeManager.Current.Surface;
    }
}
