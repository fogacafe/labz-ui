using Xunit;

namespace Labz.UI.Validation.Tests;

public class ValidationTests
{
    [Fact]
    public void WarningDoesNotInvalidate()
    {
        var result = new ValidationResult([new("Ticker", "Review value", ValidationSeverity.Warning)]);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ErrorInvalidates()
    {
        var result = new ValidationResult([new("Ticker", "Required", ValidationSeverity.Error)]);
        Assert.False(result.IsValid);
    }
}
