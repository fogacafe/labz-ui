using Labz.UI.Core.Theming;
using System.Drawing;
using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>
/// Binds Presentation loading controls to the existing Labz UI theme manager.
/// Own one binding per screen and dispose it when the screen closes.
/// </summary>
public sealed class ScreenThemeBinding : IDisposable
{
    private readonly ScreenLoadingOverlay? _overlay;
    private readonly ScreenLoadingIndicator? _indicator;
    private bool _disposed;

    public ScreenThemeBinding(ScreenLoadingOverlay? overlay = null, ScreenLoadingIndicator? indicator = null)
    {
        if (overlay is null && indicator is null)
            throw new ArgumentException("At least one loading control must be provided.");
        _overlay = overlay;
        _indicator = indicator;
        UiThemeManager.ThemeChanged += OnThemeChanged;
        Apply();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;
        var control = (Control?)_overlay ?? _indicator;
        if (control is null || control.IsDisposed) return;
        if (control.InvokeRequired)
        {
            if (control.IsHandleCreated)
                try { control.BeginInvoke((Action)Apply); }
                catch (InvalidOperationException) { /* Handle was destroyed. */ }
            return;
        }
        Apply();
    }

    public void Apply()
    {
        if (_disposed) return;
        var dark = ReferenceEquals(UiThemeManager.Current, UiTheme.Dark);
        if (_overlay is { IsDisposed: false } overlay)
        {
            if (dark) overlay.UseDarkPalette();
            else overlay.UseLightPalette();
        }
        if (_indicator is { IsDisposed: false } indicator)
        {
            indicator.BackColor = dark ? Color.FromArgb(29, 32, 39) : Color.FromArgb(245, 246, 249);
            indicator.ForeColor = dark ? Color.FromArgb(237, 239, 244) : Color.FromArgb(38, 43, 53);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UiThemeManager.ThemeChanged -= OnThemeChanged;
    }
}
