using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>
/// A small reusable WinForms loading indicator bound to a screen-local loading manager.
/// </summary>
public sealed class ScreenLoadingIndicator : UserControl
{
    private readonly Label _label = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private IScreenLoadingManager? _manager;

    public ScreenLoadingIndicator()
    {
        Height = 30;
        Dock = DockStyle.Top;
        Controls.Add(_label);
        _label.Text = "Ready";
    }

    public void Bind(IScreenLoadingManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        if (ReferenceEquals(_manager, manager)) return;
        if (_manager is not null) _manager.Changed -= OnChanged;
        _manager = manager;
        _manager.Changed += OnChanged;
        RefreshState();
    }

    private void OnChanged(object? sender, EventArgs args)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            if (IsHandleCreated)
            {
                try { BeginInvoke((Action)RefreshState); }
                catch (InvalidOperationException) { /* Handle may be destroyed during close. */ }
            }
            return;
        }
        RefreshState();
    }

    private void RefreshState()
    {
        if (IsDisposed || Disposing || _manager is null) return;
        _label.Text = _manager.IsLoading
            ? $"Loading ({_manager.ActiveCount}): {_manager.Message}"
            : "Ready";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _manager is not null)
        {
            _manager.Changed -= OnChanged;
            _manager = null;
        }
        base.Dispose(disposing);
    }
}
