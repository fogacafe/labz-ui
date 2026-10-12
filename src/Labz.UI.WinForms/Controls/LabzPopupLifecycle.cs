namespace Labz.UI.WinForms.Controls;

/// <summary>
/// Dismisses a floating dropdown whenever its anchor moves, scrolls, hides or
/// leaves the foreground window. Attach only while opening; release on Closed.
/// </summary>
internal sealed class LabzPopupLifecycle : IDisposable
{
    private readonly Control _anchor;
    private readonly ToolStripDropDown _popup;
    private readonly List<Control> _ancestors = [];
    private readonly List<ScrollableControl> _scrollParents = [];
    private Form? _owner;
    private bool _disposed;

    public LabzPopupLifecycle(Control anchor, ToolStripDropDown popup)
    {
        _anchor = anchor;
        _popup = popup;
        _popup.Closed += OnPopupClosed;
    }

    public void Attach()
    {
        if (_disposed) return;
        Detach();
        _anchor.LocationChanged += OnAnchorChanged;
        _anchor.SizeChanged += OnAnchorChanged;
        _anchor.VisibleChanged += OnAnchorChanged;
        _anchor.ParentChanged += OnAnchorChanged;
        for (Control? parent = _anchor.Parent; parent is not null; parent = parent.Parent)
        {
            parent.LocationChanged += OnAnchorChanged;
            parent.VisibleChanged += OnAnchorChanged;
            _ancestors.Add(parent);
            if (parent is ScrollableControl scroll)
            {
                scroll.Scroll += OnScrolled;
                _scrollParents.Add(scroll);
            }
        }
        _owner = _anchor.FindForm();
        if (_owner is not null)
        {
            _owner.Move += OnAnchorChanged;
            _owner.Resize += OnAnchorChanged;
            _owner.FormClosed += OnOwnerClosed;
            _owner.Deactivate += OnOwnerDeactivated;
        }
    }

    private void OnScrolled(object? sender, ScrollEventArgs e) => _popup.Close();
    private void OnAnchorChanged(object? sender, EventArgs e) => _popup.Close();
    private void OnOwnerClosed(object? sender, FormClosedEventArgs e) => _popup.Close();
    private void OnOwnerDeactivated(object? sender, EventArgs e) => _popup.Close();
    private void OnPopupClosed(object? sender, ToolStripDropDownClosedEventArgs e) => Detach();

    private void Detach()
    {
        _anchor.LocationChanged -= OnAnchorChanged;
        _anchor.SizeChanged -= OnAnchorChanged;
        _anchor.VisibleChanged -= OnAnchorChanged;
        _anchor.ParentChanged -= OnAnchorChanged;
        foreach (var parent in _ancestors)
        {
            parent.LocationChanged -= OnAnchorChanged;
            parent.VisibleChanged -= OnAnchorChanged;
        }
        _ancestors.Clear();
        foreach (var scroll in _scrollParents) scroll.Scroll -= OnScrolled;
        _scrollParents.Clear();
        if (_owner is not null)
        {
            _owner.Move -= OnAnchorChanged;
            _owner.Resize -= OnAnchorChanged;
            _owner.FormClosed -= OnOwnerClosed;
            _owner.Deactivate -= OnOwnerDeactivated;
            _owner = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Detach();
        _popup.Closed -= OnPopupClosed;
    }
}
