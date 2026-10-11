using System.ComponentModel;
using System.Globalization;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware native date/time picker with culture-aware formatting.</summary>
public class LabzDatePicker : DateTimePicker
{
    private bool _showTime;
    public LabzDatePicker()
    {
        Format = DateTimePickerFormat.Custom;
        CustomFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        Height = 42;
        CalendarForeColor = UiThemeManager.Current.Foreground;
        CalendarMonthBackground = UiThemeManager.Current.Surface;
        ApplyTheme();
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool ShowTime
    {
        get => _showTime;
        set
        {
            _showTime = value;
            Format = DateTimePickerFormat.Custom;
            CustomFormat = value
                ? CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern
                : CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
        }
    }

    private void ApplyTheme()
    {
        var theme = UiThemeManager.Current;
        CalendarForeColor = theme.Foreground;
        CalendarMonthBackground = theme.Surface;
        CalendarTitleBackColor = theme.Primary;
        CalendarTitleForeColor = Color.White;
        BackColor = theme.Surface;
        ForeColor = theme.Foreground;
        Invalidate();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            if (!IsHandleCreated) return;
            try { BeginInvoke((Action)ApplyTheme); }
            catch (InvalidOperationException) { /* Closing handle. */ }
        }
        else ApplyTheme();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) UiThemeManager.ThemeChanged -= OnThemeChanged;
        base.Dispose(disposing);
    }
}
