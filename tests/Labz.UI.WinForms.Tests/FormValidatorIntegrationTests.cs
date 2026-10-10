using System.Diagnostics;
using System.Threading;
using Labz.UI.Validation;
using Labz.UI.WinForms.Controls;
using Labz.UI.WinForms.Validation;
using Xunit;

namespace Labz.UI.WinForms.Tests;

/// <summary>Runs real WinForms controls on a dedicated STA thread with a message pump.</summary>
public class FormValidatorIntegrationTests
{
    [Fact]
    public void ChangeTriggerDebouncesRapidTyping()
    {
        RunSta(() =>
        {
            using var form = new Form();
            using var input = new LabzTextBox();
            form.Controls.Add(input);
            form.Show();
            var calls = 0;
            using var validation = new LabzFormValidator();
            validation.Register(input, new FieldValidator("Name").Must(value =>
            {
                ++calls;
                return value == "ABC";
            }, "Invalid"), ValidationTrigger.Change, TimeSpan.FromMilliseconds(80));

            input.Text = "A";
            input.Text = "AB";
            input.Text = "ABC";
            PumpUntil(() => calls == 1);
            Assert.Equal(1, calls);
            Assert.Equal(ValidationSeverity.None, input.ValidationState);
        });
    }

    [Fact]
    public void SubmitRunsWithoutWaitingForDebounce()
    {
        RunSta(() =>
        {
            using var form = new Form();
            using var input = new LabzTextBox();
            form.Controls.Add(input);
            form.Show();
            using var validation = new LabzFormValidator();
            validation.Register(input, new FieldValidator("Name").NotEmpty(),
                ValidationTrigger.Change, TimeSpan.FromSeconds(10));

            var task = validation.ValidateAsync();
            PumpUntil(() => task.IsCompleted);
            Assert.False(task.GetAwaiter().GetResult().IsValid);
            Assert.Equal(ValidationSeverity.Error, input.ValidationState);
        });
    }

    [Fact]
    public void OlderAsyncResponseCannotOverwriteNewerValue()
    {
        RunSta(() =>
        {
            using var form = new Form();
            using var input = new LabzTextBox();
            form.Controls.Add(input);
            form.Show();
            var first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var validation = new LabzFormValidator();
            validation.Register(input, new FieldValidator("Name")
                .MustAsync((value, _) => value == "old" ? first.Task : Task.FromResult(true),
                    "Old result"), ValidationTrigger.Change, TimeSpan.Zero);

            input.Text = "old";
            input.Text = "new";
            PumpUntil(() => input.ValidationState == ValidationSeverity.None);
            first.SetResult(false);
            PumpFor(TimeSpan.FromMilliseconds(100));
            Assert.Equal(ValidationSeverity.None, input.ValidationState);
        });
    }

    [Fact]
    public void DisposeStopsPendingChangeValidation()
    {
        RunSta(() =>
        {
            using var form = new Form();
            using var input = new LabzTextBox();
            form.Controls.Add(input);
            form.Show();
            var calls = 0;
            var validation = new LabzFormValidator();
            validation.Register(input, new FieldValidator("Name").Must(_ =>
            {
                ++calls;
                return false;
            }, "Invalid"), ValidationTrigger.Change, TimeSpan.FromMilliseconds(150));
            input.Text = "test";
            validation.Dispose();
            PumpFor(TimeSpan.FromMilliseconds(250));
            Assert.Equal(0, calls);
        });
    }

    [Fact]
    public void FailedAsyncValidationDisplaysSafeError()
    {
        RunSta(() =>
        {
            using var form = new Form();
            using var input = new LabzTextBox();
            form.Controls.Add(input);
            form.Show();
            using var validation = new LabzFormValidator();
            validation.Register(input, new FieldValidator("Name")
                .MustAsync((_, _) => Task.FromException<bool>(new InvalidOperationException("Remote failure")),
                    "Invalid"), ValidationTrigger.Change, TimeSpan.Zero);

            input.Text = "test";
            PumpUntil(() => input.ValidationState == ValidationSeverity.Error);
            Assert.Equal("Validation could not be completed", input.ValidationMessage);
        });
    }

    private static void RunSta(Action test)
    {
        Exception? failure = null;
        using var finished = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            try
            {
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                test();
            }
            catch (Exception ex) { failure = ex; }
            finally { finished.Set(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(finished.Wait(TimeSpan.FromSeconds(15)), "STA test timed out.");
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(5))
                throw new TimeoutException("Condition not reached.");
            Application.DoEvents();
            Thread.Sleep(5);
        }
    }

    private static void PumpFor(TimeSpan duration)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < duration)
        {
            Application.DoEvents();
            Thread.Sleep(5);
        }
    }
}
