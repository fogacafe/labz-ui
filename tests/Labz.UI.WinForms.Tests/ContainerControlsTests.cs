using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class ContainerControlsTests
{
    [Fact]
    public void AccordionCollapseAndExpandPreserveContent()
    {
        RunSta(() =>
        {
            using var accordion = new LabzAccordion { Title = "Advanced", ExpandedHeight = 200 };
            using var child = new TextBox();
            accordion.ContentPanel.Controls.Add(child);
            var changed = 0;
            accordion.ExpandedChanged += (_, _) => changed++;
            accordion.Expanded = false;
            Assert.False(accordion.Expanded);
            Assert.Equal(40, accordion.Height);
            Assert.Same(accordion.ContentPanel, child.Parent);
            accordion.Expanded = true;
            Assert.Equal(200, accordion.Height);
            Assert.Equal(2, changed);
        });
    }

    [Fact]
    public void GroupBoxValidatesRadius()
    {
        RunSta(() =>
        {
            using var box = new LabzGroupBox { Text = "Details", CornerRadius = 12 };
            Assert.Equal(12, box.CornerRadius);
            Assert.Throws<ArgumentOutOfRangeException>(() => box.CornerRadius = -1);
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
