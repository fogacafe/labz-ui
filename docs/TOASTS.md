# Toast notifications

`LabzToast` provides non-modal, owner-anchored notifications. Toasts stack
upwards near the lower-right of their owning form and reposition when it moves
or resizes. Each toast follows the active light/dark theme.

```csharp
using Labz.UI.WinForms.Notifications;

LabzToast.Success(this, "Saved successfully!");
LabzToast.Warning(this, "Review this field", TimeSpan.FromSeconds(8));
LabzToast.Error(this, "Connection failed");
LabzToast.Info(this, "Background sync started");

// Persistent toast: dismiss using its close button.
LabzToast.Show(this, "Long-running task", duration: TimeSpan.Zero);
```

The default lifetime is four seconds. Closing the owner disposes its toasts.
Calls from worker threads are marshaled onto the owner form's UI thread.
A toast does not block input or activate its window.

A visual Windows smoke test is recommended for monitor edges, high DPI,
multiple simultaneous toasts and accessibility.
