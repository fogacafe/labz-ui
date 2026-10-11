using System.Globalization;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class AdvancedInputsTests
{
    [Fact]
    public void NumericInputPreservesDecimalValueAcrossFormats()
    {
        RunSta(() =>
        {
            using var input = new LabzNumericInput { Culture = CultureInfo.GetCultureInfo("pt-BR"), Value = 1234.56m };
            input.NumericFormat = LabzNumericFormat.Currency;
            Assert.Equal(1234.56m, input.Value);
            Assert.Contains("R$", input.Text);
            input.NumericFormat = LabzNumericFormat.Percent;
            Assert.Equal(1234.56m, input.Value);
            Assert.Contains("%", input.Text);
        });
    }

    [Fact]
    public void NumericInputUsesCustomChromeAndRespectsBounds()
    {
        RunSta(() =>
        {
            using var input = new LabzNumericInput
            {
                Minimum = -10m,
                Maximum = 10m,
                Increment = 2m,
                Value = 9m
            };
            Assert.IsAssignableFrom<UserControl>(input);
            Assert.DoesNotContain(input.Controls.Cast<Control>(), control => control is NumericUpDown);
            Assert.Contains(input.Controls.Cast<Control>(), control => control is TextBox);
            input.UpButton();
            Assert.Equal(10m, input.Value);
            input.DownButton();
            Assert.Equal(8m, input.Value);
        });
    }

    [Fact]
    public void DatePickerSupportsDateAndTime()
    {
        RunSta(() =>
        {
            using var input = new LabzDatePicker { ShowTime = true };
            Assert.True(input.ShowTime);
            Assert.IsAssignableFrom<UserControl>(input);
            Assert.DoesNotContain(input.Controls.Cast<Control>(), control => control is DateTimePicker);
            input.ShowTime = false;
            Assert.Equal(DateTimePickerFormat.Custom, input.Format);
            Assert.Equal(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, input.CustomFormat);
        });
    }

    [Fact]
    public void DatePickerClampsValueAndPreservesTime()
    {
        RunSta(() =>
        {
            using var input = new LabzDatePicker
            {
                MinDate = new DateTime(2026, 1, 1),
                MaxDate = new DateTime(2026, 12, 31, 23, 59, 59),
                ShowTime = true
            };
            input.Value = new DateTime(2026, 5, 12, 14, 30, 0);
            Assert.Equal(new DateTime(2026, 5, 12, 14, 30, 0), input.Value);
            input.Value = new DateTime(2027, 1, 1);
            Assert.Equal(input.MaxDate, input.Value);
        });
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
