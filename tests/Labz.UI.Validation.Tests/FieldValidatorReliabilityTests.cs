using Xunit;

namespace Labz.UI.Validation.Tests;

public class FieldValidatorReliabilityTests
{
    [Fact]
    public async Task RulesRunInRegistrationOrder()
    {
        var calls = new List<string>();
        var validator = new FieldValidator("Code")
            .Must(value => { calls.Add("first"); return false; }, "First")
            .Must(value => { calls.Add("second"); return false; }, "Second");

        var result = await validator.ValidateAsync("x");

        Assert.Equal(["first", "second"], calls);
        Assert.Equal(["First", "Second"], result.Messages.Select(x => x.Message));
    }

    [Fact]
    public async Task AsyncRulesReceiveCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken received = default;
        var validator = new FieldValidator("Code")
            .MustAsync((_, token) =>
            {
                received = token;
                return Task.FromResult(true);
            }, "Invalid");

        await validator.ValidateAsync("x", cts.Token);

        Assert.Equal(cts.Token, received);
    }

    [Fact]
    public async Task CancellationBetweenRulesStopsRemainingRules()
    {
        using var cts = new CancellationTokenSource();
        var invoked = false;
        var validator = new FieldValidator("Code")
            .Must(value => { cts.Cancel(); return true; }, "First")
            .Must(value => { invoked = true; return true; }, "Second");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => validator.ValidateAsync("x", cts.Token));

        Assert.False(invoked);
    }

    [Fact]
    public async Task FailedAsyncRuleProducesErrorMessage()
    {
        var validator = new FieldValidator("Code")
            .MustAsync((_, _) => Task.FromResult(false), "Invalid code");

        var result = await validator.ValidateAsync("x");

        var message = Assert.Single(result.Messages);
        Assert.Equal(ValidationSeverity.Error, message.Severity);
        Assert.Equal("Code", message.PropertyName);
    }

    [Fact]
    public async Task ExceptionsFromAsyncRulesAreNotSilentlySwallowed()
    {
        var validator = new FieldValidator("Code")
            .MustAsync((_, _) => Task.FromException<bool>(new InvalidOperationException("Service down")),
                "Unavailable");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.ValidateAsync("x"));
    }
}
