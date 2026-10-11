using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class FeedbackControlsTests
{
    [Fact]
    public void SwitchRaisesNativeCheckedChanged()
    {
        RunSta(() =>
        {
            using var toggle = new LabzSwitch();
            var changes = 0;
            toggle.CheckedChanged += (_, _) => changes++;
            toggle.Checked = true;
            Assert.True(toggle.Checked);
            Assert.Equal(1, changes);
        });
    }

    [Fact]
    public void ProgressRetainsNativeRangeSemantics()
    {
        RunSta(() =>
        {
            using var progress = new LabzProgressBar { Minimum = 10, Maximum = 110, Value = 60, ShowPercentage = true };
            Assert.Equal(60, progress.Value);
            Assert.True(progress.ShowPercentage);
        });
    }

    [Fact]
    public void SeparatorRejectsInvalidThickness()
    {
        RunSta(() =>
        {
            using var separator = new LabzSeparator { Orientation = LabzSeparatorOrientation.Vertical, Thickness = 2 };
            Assert.Equal(2, separator.Thickness);
            Assert.Throws<ArgumentOutOfRangeException>(() => separator.Thickness = 0);
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
