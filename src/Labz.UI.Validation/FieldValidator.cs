namespace Labz.UI.Validation;

/// <summary>Composable synchronous and asynchronous validation rules for one string field.</summary>
public sealed class FieldValidator
{
    private readonly List<Func<string, CancellationToken, ValueTask<ValidationMessage?>>> _rules = [];

    public FieldValidator(string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        FieldName = fieldName;
    }

    public string FieldName { get; }

    public FieldValidator NotEmpty(string message = "This field is required")
    {
        return Must(value => !string.IsNullOrWhiteSpace(value), message);
    }

    public FieldValidator MinLength(int length, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        return Must(value => value.Length >= length, message ?? $"Minimum length is {length}");
    }

    public FieldValidator Must(Func<string, bool> predicate, string message,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(message);
        _rules.Add((value, _) => ValueTask.FromResult<ValidationMessage?>(
            predicate(value) ? null : new ValidationMessage(FieldName, message, severity)));
        return this;
    }

    public FieldValidator MustAsync(Func<string, CancellationToken, Task<bool>> predicate,
        string message, ValidationSeverity severity = ValidationSeverity.Error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(message);
        _rules.Add(async (value, ct) =>
            await predicate(value, ct).ConfigureAwait(false)
                ? null : new ValidationMessage(FieldName, message, severity));
        return this;
    }

    public async Task<ValidationResult> ValidateAsync(string? value,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ValidationMessage>();
        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = await rule(value ?? string.Empty, cancellationToken).ConfigureAwait(false);
            if (message is not null) messages.Add(message);
        }
        return new ValidationResult(messages);
    }
}
