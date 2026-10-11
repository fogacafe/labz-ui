using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class VisualComponentsTests
{
    [Fact]
    public void BadgeSupportsSemanticVariants()
    {
        RunSta(() =>
        {
            using var badge = new LabzBadge { Text = "Completed", Variant = LabzBadgeVariant.Success };
            Assert.Equal("Completed", badge.Text);
            Assert.Equal(LabzBadgeVariant.Success, badge.Variant);
        });
    }

    [Fact]
    public void CardCanContainContentAndHeading()
    {
        RunSta(() =>
        {
            using var card = new LabzCard { Title = "Overview" };
            card.Controls.Add(new Label { Text = "Content" });
            Assert.Equal("Overview", card.Title);
            Assert.Equal(2, card.Controls.Count);
        });
    }

    [Fact]
    public void SpinnerCanBePaused()
    {
        RunSta(() =>
        {
            using var spinner = new LabzSpinner { Active = false };
            Assert.False(spinner.Active);
        });
    }

    [Fact]
    public void EmptyStateRetainsItsMessage()
    {
        RunSta(() =>
        {
            using var empty = new LabzEmptyState { Heading = "Nothing found", Description = "Try again" };
            Assert.Equal("Nothing found", empty.Heading);
            Assert.Equal("Try again", empty.Description);
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
