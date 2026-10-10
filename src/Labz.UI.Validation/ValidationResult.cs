namespace Labz.UI.Validation;

public enum ValidationSeverity { None, Success, Warning, Error }

public sealed record ValidationMessage(
    string PropertyName,
    string Message,
    ValidationSeverity Severity);

public sealed record ValidationResult(IReadOnlyList<ValidationMessage> Messages)
{
    public bool IsValid => Messages.All(message => message.Severity != ValidationSeverity.Error);
}
