using System.Threading;
using Labz.UI.WinForms.Controls;
using Xunit;

namespace Labz.UI.WinForms.Tests;

public class SearchLookupTests
{
    [Fact]
    public void LookupStartsWithSensibleDefaults()
    {
        RunSta(() =>
        {
            using var lookup = new LabzSearchLookup();
            Assert.Equal(10, lookup.MaxResults);
            Assert.Equal(350, lookup.DebounceMilliseconds);
            Assert.Null(lookup.SelectedItem);
        });
    }

    [Fact]
    public void ProviderResultsAreCappedAndColumnsCanBeConfigured()
    {
        RunSta(() =>
        {
            using var lookup = new LabzSearchLookup { MaxResults = 2 };
            lookup.Columns = [new LabzLookupColumn("code", "Code"), new LabzLookupColumn("name", "Name")];
            lookup.SearchProvider = (_, _, _) => Task.FromResult<IReadOnlyList<LabzLookupItem>>(
                Enumerable.Range(1, 5).Select(i => new LabzLookupItem(i, $"Item {i}",
                    new Dictionary<string, object?> { ["code"] = i, ["name"] = $"Item {i}" })).ToArray());
            lookup.SearchAsync().GetAwaiter().GetResult();
            Assert.Equal(2, lookup.Results.Count);
            Assert.Equal(2, lookup.Results[0].Values.Count);
        });
    }

    [Fact]
    public void SearchInputAndResultsHaveAccessibleNames()
    {
        RunSta(() =>
        {
            using var lookup = new LabzSearchLookup();
            Assert.Equal("Lookup search", lookup.Controls.OfType<TextBox>().Single().AccessibleName);
            Assert.Empty(lookup.Controls.OfType<Button>()); // Search icon is custom-painted, not a native button.
        });
    }

    [Fact]
    public void InvalidDebounceAndResultLimitAreRejected()
    {
        RunSta(() =>
        {
            using var lookup = new LabzSearchLookup();
            Assert.Throws<ArgumentOutOfRangeException>(() => lookup.MaxResults = 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => lookup.DebounceMilliseconds = 0);
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
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)));
        if (error is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
    }
}
