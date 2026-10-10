using Xunit;

namespace Labz.UI.Validation.Tests;

public class FieldValidatorTests
{
    [Fact]
    public async Task RequiredFieldReturnsError()
    {
        var validator = new FieldValidator("Ticker").NotEmpty();
        var result = await validator.ValidateAsync(" ");
        Assert.False(result.IsValid);
        Assert.Equal("Ticker", Assert.Single(result.Messages).PropertyName);
    }

    [Fact]
    public async Task ValidInputHasNoMessages()
    {
        var validator = new FieldValidator("Ticker").NotEmpty().MinLength(3);
        Assert.Empty((await validator.ValidateAsync("PETR4")).Messages);
    }

    [Fact]
    public async Task AsyncRuleReturnsWarning()
    {
        var validator = new FieldValidator("Ticker")
            .MustAsync((value, ct) => Task.FromResult(value == "PETR4"),
                "Unknown ticker", ValidationSeverity.Warning);
        var result = await validator.ValidateAsync("UNKNOWN");
        Assert.True(result.IsValid);
        Assert.Equal(ValidationSeverity.Warning, Assert.Single(result.Messages).Severity);
    }

    [Fact]
    public async Task CancellationIsHonored()
    {
        var validator = new FieldValidator("Ticker").NotEmpty();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => validator.ValidateAsync("ABC", cts.Token));
    }
}
