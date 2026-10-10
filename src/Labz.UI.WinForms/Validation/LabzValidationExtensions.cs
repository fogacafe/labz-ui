using Labz.UI.Validation;
using Labz.UI.WinForms.Controls;

namespace Labz.UI.WinForms.Validation;

public static class LabzValidationExtensions
{
    /// <summary>Applies the first highest-severity field message to each named input.</summary>
    public static void ApplyValidation(
        this IEnumerable<LabzTextBox> controls,
        ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(controls);
        ArgumentNullException.ThrowIfNull(result);

        var messages = result.Messages
            .GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(message => message.Severity).First(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var control in controls)
        {
            if (messages.TryGetValue(control.Name, out var message))
                control.ApplyValidation(message);
            else
                control.ClearValidation();
        }
    }
}
