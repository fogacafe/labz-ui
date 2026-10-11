using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class BasicControlsTests
{
    [Fact]
    public void CheckBoxPreservesThreeStateAndEvents()
    {
        RunSta(() =>
        {
            using var check = new LabzCheckBox { ThreeState = true };
            var changed = 0;
            check.CheckStateChanged += (_, _) => changed++;
            check.CheckState = CheckState.Indeterminate;
            Assert.Equal(CheckState.Indeterminate, check.CheckState);
            Assert.Equal(1, changed);
        });
    }

    [Fact]
    public void RadioButtonsRetainNativeExclusiveGrouping()
    {
        RunSta(() =>
        {
            using var panel = new Panel();
            using var first = new LabzRadioButton { Text = "A" };
            using var second = new LabzRadioButton { Text = "B" };
            panel.Controls.AddRange([first, second]);
            first.Checked = true;
            second.Checked = true;
            Assert.False(first.Checked);
            Assert.True(second.Checked);
        });
    }

    [Fact]
    public void LabelVariantCanChange()
    {
        RunSta(() =>
        {
            using var label = new LabzLabel { Text = "Section", Variant = LabzLabelVariant.Heading };
            Assert.True(label.Font.Bold);
            label.Variant = LabzLabelVariant.Caption;
            Assert.False(label.Font.Bold);
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
