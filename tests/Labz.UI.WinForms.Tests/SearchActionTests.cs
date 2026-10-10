using System.Threading;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class SearchActionTests
{
    [Fact]
    public void TextBoxSearchButtonIsOptIn()
    {
        RunSta(() =>
        {
            using var input = new LabzTextBox();
            Assert.False(input.ShowSearchButton);
            input.ShowSearchButton = true;
            Assert.True(input.ShowSearchButton);
            Assert.Equal("Search", Assert.Single(input.Controls.OfType<Button>()).AccessibleName);
        });
    }

    [Fact]
    public void ComboBoxPreservesNativeSelection()
    {
        RunSta(() =>
        {
            using var input = new LabzComboBox();
            input.Items.AddRange(["One", "Two"]);
            input.SelectedIndex = 1;
            Assert.Equal("Two", input.SelectedItem);
            Assert.Equal("Two", input.Text);
            input.ShowSearchButton = true;
            Assert.True(input.ShowSearchButton);
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "STA test timed out");
        if (error is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
