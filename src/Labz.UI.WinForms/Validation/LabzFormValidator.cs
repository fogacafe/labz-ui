using Labz.UI.Validation;
using Labz.UI.WinForms.Controls;

namespace Labz.UI.WinForms.Validation;

[Flags]
public enum ValidationTrigger
{
    None = 0,
    Change = 1,
    Blur = 2
}

/// <summary>
/// Connects independent validation rules to WinForms inputs.
/// Dispose with the containing Form to unsubscribe from control events.
/// </summary>
public sealed class LabzFormValidator : IDisposable
{
    private readonly Dictionary<LabzTextBox, Registration> _registrations = [];
    private bool _disposed;

    public LabzFormValidator Register(
        LabzTextBox control, FieldValidator validator,
        ValidationTrigger triggers = ValidationTrigger.Blur,
        TimeSpan? debounce = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(validator);
        if (_registrations.ContainsKey(control))
            throw new InvalidOperationException("The input is already registered.");

        var delay = debounce ?? TimeSpan.FromMilliseconds(350);
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(debounce));
        var registration = new Registration(control, validator, triggers, delay);
        _registrations.Add(control, registration);
        registration.Attach();
        return this;
    }

    /// <summary>Validates all registered fields, including asynchronous rules.</summary>
    public async Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var messages = new List<ValidationMessage>();
        foreach (var registration in _registrations.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await registration.ValidateAsync(cancellationToken);
            messages.AddRange(result.Messages);
        }
        return new ValidationResult(messages);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var registration in _registrations.Values) registration.Dispose();
        _registrations.Clear();
    }

    private sealed class Registration : IDisposable
    {
        private readonly LabzTextBox _control;
        private readonly FieldValidator _validator;
        private readonly ValidationTrigger _triggers;
        private CancellationTokenSource? _pending;
        private long _version;
        private readonly TimeSpan _debounce;
        private bool _disposed;

        public Registration(LabzTextBox control, FieldValidator validator, ValidationTrigger triggers, TimeSpan debounce)
        {
            _control = control;
            _validator = validator;
            _triggers = triggers;
            _debounce = debounce;
        }

        public void Attach()
        {
            if (_triggers.HasFlag(ValidationTrigger.Change)) _control.TextChanged += OnChanged;
            if (_triggers.HasFlag(ValidationTrigger.Blur)) _control.Leave += OnBlurred;
        }

        private void OnChanged(object? sender, EventArgs e) => _ = ValidateOnEventAsync(_debounce);
        private void OnBlurred(object? sender, EventArgs e) => _ = ValidateOnEventAsync(TimeSpan.Zero);

        private async Task ValidateOnEventAsync(TimeSpan delay)
        {
            var expectedVersion = _version + 1;
            try { await ValidateCoreAsync(CancellationToken.None, delay); }
            catch (OperationCanceledException) { /* Superseded by newer input. */ }
            catch (Exception)
            {
                // Only the latest validation may update the field, including failures.
                if (!_disposed && expectedVersion == _version && !_control.IsDisposed)
                    _control.ApplyValidation(new ValidationMessage(_validator.FieldName,
                        "Validation could not be completed", ValidationSeverity.Error));
            }
        }

        public Task<ValidationResult> ValidateAsync(CancellationToken cancellationToken) =>
            ValidateCoreAsync(cancellationToken, TimeSpan.Zero);

        private async Task<ValidationResult> ValidateCoreAsync(CancellationToken cancellationToken, TimeSpan delay)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _pending?.Cancel();
            var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _pending = source;
            var version = ++_version;
            var text = _control.Text;
            try
            {
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, source.Token);
                var result = await _validator.ValidateAsync(text, source.Token);
                if (!_disposed && version == _version && !_control.IsDisposed && !source.IsCancellationRequested)
                {
                    var message = result.Messages.OrderByDescending(x => x.Severity).FirstOrDefault();
                    _control.ApplyValidation(message);
                }
                return result;
            }
            finally
            {
                if (ReferenceEquals(_pending, source)) _pending = null;
                source.Dispose();
            }
        }

        public void Dispose()
        {
            _disposed = true;
            ++_version;
            _control.TextChanged -= OnChanged;
            _control.Leave -= OnBlurred;
            _pending?.Cancel();
        }
    }
}
