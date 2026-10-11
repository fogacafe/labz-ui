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
    public void DatePickerSupportsDateAndTime()
    {
        RunSta(() =>
        {
            using var input = new LabzDatePicker { ShowTime = true };
            Assert.True(input.ShowTime);
            input.ShowTime = false;
            Assert.Equal(DateTimePickerFormat.Custom, input.Format);
            Assert.Equal(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, input.CustomFormat);
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
