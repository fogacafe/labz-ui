using Labz.UI.Core.Theming;
using Labz.UI.WinForms.Controls;

namespace Labz.UI.WinForms.Notifications;

public enum LabzToastKind { Information, Success, Warning, Error }

/// <summary>Non-modal notifications stacked within an owning WinForms window.</summary>
public static class LabzToast
{
    private static readonly Dictionary<Form, List<ToastWindow>> Active = new();
    private const int Margin = 18;
    private const int Gap = 10;

    public static void Show(Form owner, string message, LabzToastKind kind = LabzToastKind.Information,
        TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(message);
        if (owner.IsDisposed || owner.Disposing)
            throw new ObjectDisposedException(nameof(owner));
        if (owner.InvokeRequired)
        {
            owner.BeginInvoke((Action)(() => Show(owner, message, kind, duration)));
            return;
        }
        if (!owner.IsHandleCreated)
            throw new InvalidOperationException("Show the owner form before displaying a toast.");
        var lifetime = duration ?? TimeSpan.FromSeconds(4);
        if (lifetime < TimeSpan.Zero || lifetime.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(duration));

        if (!Active.TryGetValue(owner, out var stack))
        {
            stack = [];
            Active.Add(owner, stack);
            owner.Move += OwnerChanged;
            owner.Resize += OwnerChanged;
            owner.FormClosed += OwnerClosed;
        }
        var toast = new ToastWindow(message, kind, lifetime, () => Remove(owner));
        stack.Add(toast);
        Position(owner);
        toast.Show(owner);
        toast.BeginLifetime();
    }

    public static void Success(Form owner, string message, TimeSpan? duration = null)
        => Show(owner, message, LabzToastKind.Success, duration);

    public static void Warning(Form owner, string message, TimeSpan? duration = null)
        => Show(owner, message, LabzToastKind.Warning, duration);

    public static void Error(Form owner, string message, TimeSpan? duration = null)
        => Show(owner, message, LabzToastKind.Error, duration);

    public static void Info(Form owner, string message, TimeSpan? duration = null)
        => Show(owner, message, LabzToastKind.Information, duration);

    private static void Remove(Form owner)
    {
        if (!Active.TryGetValue(owner, out var stack)) return;
        stack.RemoveAll(t => t.IsDisposed || !t.Visible);
        if (stack.Count == 0)
        {
            Detach(owner);
            return;
        }
        Position(owner);
    }

    private static void Position(Form owner)
    {
        if (!Active.TryGetValue(owner, out var stack)) return;
        var area = Screen.FromControl(owner).WorkingArea;
        var right = Math.Min(owner.Right - Margin, area.Right - Margin);
        var bottom = Math.Min(owner.Bottom - Margin, area.Bottom - Margin);
        for (var i = stack.Count - 1; i >= 0; i--)
        {
            var toast = stack[i];
            if (toast.IsDisposed) continue;
            bottom -= toast.Height;
            toast.Location = new Point(Math.Max(area.Left + Margin, right - toast.Width),
                Math.Max(area.Top + Margin, bottom));
            bottom -= Gap;
        }
    }

    private static void OwnerChanged(object? sender, EventArgs e)
    {
        if (sender is Form owner) Position(owner);
    }

    private static void OwnerClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is not Form owner || !Active.TryGetValue(owner, out var stack)) return;
        foreach (var toast in stack.ToArray()) toast.Dispose();
        Detach(owner);
    }

    private static void Detach(Form owner)
    {
        owner.Move -= OwnerChanged;
        owner.Resize -= OwnerChanged;
        owner.FormClosed -= OwnerClosed;
        Active.Remove(owner);
    }

    private sealed class ToastWindow : Form
    {
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 25 };
        private readonly Action _onClose;
        private readonly TimeSpan _duration;
        private readonly LabzToastKind _kind;
        private readonly Label _label = new()
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 8, 8, 8), AutoEllipsis = true
        };
        private readonly LabzButton _close = new()
        {
            Text = "×", Width = 36, Height = 32,
            Variant = LabzButtonVariant.Ghost, AccessibleName = "Dismiss notification"
        };
        private int _elapsed;
        private bool _closing;

        public ToastWindow(string message, LabzToastKind kind, TimeSpan duration, Action onClose)
        {
            _duration = duration;
            _kind = kind;
            _onClose = onClose;
            Text = kind.ToString();
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = false;
            Size = new Size(330, 78);
            MinimumSize = new Size(260, 70);
            _label.Text = message;
            _label.AccessibleName = kind + " notification";
            _close.Dock = DockStyle.Right;
            _close.Click += (_, _) => Close();
            Controls.Add(_label);
            Controls.Add(_close);
            UiThemeManager.ThemeChanged += ThemeChanged;
            ApplyTheme();
            _timer.Tick += (_, _) =>
            {
                _elapsed += _timer.Interval;
                if (_duration > TimeSpan.Zero && _elapsed >= _duration.TotalMilliseconds)
                {
                    _timer.Stop();
                    Close();
                }
                else if (_duration > TimeSpan.Zero && _elapsed <= 175)
                    Opacity = Math.Min(1, _elapsed / 175d);
            };
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return parameters;
            }
        }

        public void BeginLifetime()
        {
            if (_duration == TimeSpan.Zero) return; // Persistent until manually dismissed.
            Opacity = .15;
            _timer.Start();
        }

        private void ApplyTheme()
        {
            var theme = UiThemeManager.Current;
            BackColor = theme.Surface;
            _label.BackColor = theme.Surface;
            _label.ForeColor = _kind switch
            {
                LabzToastKind.Error => theme.Danger,
                LabzToastKind.Warning => Color.DarkOrange,
                LabzToastKind.Success => Color.ForestGreen,
                _ => theme.Foreground
            };
            _close.BackColor = theme.Surface;
            Invalidate(true);
        }

        private void ThemeChanged(object? sender, EventArgs e)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (InvokeRequired) BeginInvoke((Action)ApplyTheme);
            else ApplyTheme();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (!_closing)
            {
                _closing = true;
                _onClose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                UiThemeManager.ThemeChanged -= ThemeChanged;
            }
            base.Dispose(disposing);
        }
    }
}
