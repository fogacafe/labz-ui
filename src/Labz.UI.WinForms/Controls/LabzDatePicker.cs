using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware native date/time picker with culture-aware formatting.</summary>
public class LabzDatePicker : DateTimePicker
{
    public LabzDatePicker()
    {
        Format = DateTimePickerFormat.Short;
        CalendarForeColor = UiThemeManager.Current.Foreground;
        CalendarMonthBackground = UiThemeManager.Current.Surface;
        ApplyTheme();
        UiThemeManager.ThemeChanged += OnThemeChanged;
    }

    [Category("Appearance")]
    [DefaultValue(false)]
    public bool ShowTime
    {
        get => Format == DateTimePickerFormat.Custom && CustomFormat == "g";
        set
        {
            Format = value ? DateTimePickerFormat.Custom : DateTimePickerFormat.Short;
            if (value) CustomFormat = "g";
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
