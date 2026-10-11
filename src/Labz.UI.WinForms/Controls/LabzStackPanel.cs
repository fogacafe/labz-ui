using System.ComponentModel;
using Labz.UI.WinForms.Layout;

namespace Labz.UI.WinForms.Controls;

/// <summary>Designer-compatible container with CSS-inspired directional layout.</summary>
[DefaultProperty(nameof(Direction))]
[ProvideProperty("FlexGrow", typeof(Control))]
[ProvideProperty("FlexShrink", typeof(Control))]
public class LabzStackPanel : Panel, IExtenderProvider
{
    private LabzDirection _direction = LabzDirection.Vertical;
    private LabzJustifyContent _justifyContent;
    private LabzAlignItems _alignItems;
    private int _spacing = 8;
    private bool _wrap;
    private bool _arranging;
    private readonly Dictionary<Control, (double Grow, double Shrink, Size Basis)> _flex = [];
    private readonly Dictionary<Control, Size> _basis = [];

    public LabzStackPanel()
    {
        SetStyle(ControlStyles.ResizeRedraw, true);
        Padding = new Padding(8);
    }

    [Category("Layout")]
    [DefaultValue(LabzDirection.Vertical)]
    public LabzDirection Direction
    {
        get => _direction;
        set { _direction = value; PerformLayout(); }
    }

    [Category("Layout")]
    [DefaultValue(8)]
    public int Spacing
    {
        get => _spacing;
        set { ArgumentOutOfRangeException.ThrowIfNegative(value); _spacing = value; PerformLayout(); }
    }

    [Category("Layout")]
    [DefaultValue(LabzJustifyContent.Start)]
    public LabzJustifyContent JustifyContent
    {
        get => _justifyContent;
        set { _justifyContent = value; PerformLayout(); }
    }

    [Category("Layout")]
    [DefaultValue(LabzAlignItems.Start)]
    public LabzAlignItems AlignItems
    {
        get => _alignItems;
        set { _alignItems = value; PerformLayout(); }
    }

    [Category("Layout")]
    [DefaultValue(false)]
    public bool Wrap
    {
        get => _wrap;
        set { _wrap = value; PerformLayout(); }
    }

    public bool CanExtend(object extendee) => extendee is Control child && child != this && child.Parent == this;

    [Category("Layout")]
    [DefaultValue(0d)]
    public double GetFlexGrow(Control child) => _flex.TryGetValue(child, out var f) ? f.Grow : 0;

    public void SetFlexGrow(Control child, double value) => SetFlex(child, value, GetFlexShrink(child));

    [Category("Layout")]
    [DefaultValue(1d)]
    public double GetFlexShrink(Control child) => _flex.TryGetValue(child, out var f) ? f.Shrink : 1;

    public void SetFlexShrink(Control child, double value) => SetFlex(child, GetFlexGrow(child), value);

    /// <summary>Sets per-child flex factors; default is grow=0, shrink=1.</summary>
    public void SetFlex(Control child, double grow = 0, double shrink = 1)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!double.IsFinite(grow) || grow < 0 || !double.IsFinite(shrink) || shrink < 0)
            throw new ArgumentOutOfRangeException(nameof(grow));
        if (child.Parent != this) throw new ArgumentException("Control must belong to this panel.", nameof(child));
        if (!_basis.TryGetValue(child, out var basis))
            _basis[child] = basis = child.Size;
        _flex[child] = (grow, shrink, basis);
        PerformLayout();
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        base.OnControlRemoved(e);
        _flex.Remove(e.Control);
        _basis.Remove(e.Control);
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        if (_arranging) return;
        _arranging = true;
        try
        {
            // Respect designer ordering (Controls order), visibility and child margins.
            var children = Controls.Cast<Control>().Where(c => c.Visible && !c.IsDisposed).Reverse().ToArray();
            var sizes = children.Select(c =>
            {
                if (!_basis.TryGetValue(c, out var basis))
                    _basis[c] = basis = c.Size;
                return new Size(basis.Width + c.Margin.Horizontal, basis.Height + c.Margin.Vertical);
            }).ToArray();
            var bounds = DisplayRectangle;
            var rectangles = LabzFlexLayoutEngine.ArrangeConstrained(sizes, bounds, Direction,
                Spacing, Wrap, JustifyContent, AlignItems,
                children.Select(GetFlexGrow).ToArray(),
                children.Select(GetFlexShrink).ToArray(),
                children.Select(c => new Size(c.MinimumSize.Width + c.Margin.Horizontal,
                    c.MinimumSize.Height + c.Margin.Vertical)).ToArray(),
                children.Select(c => new Size(
                    c.MaximumSize.Width == 0 ? 0 : c.MaximumSize.Width + c.Margin.Horizontal,
                    c.MaximumSize.Height == 0 ? 0 : c.MaximumSize.Height + c.Margin.Vertical)).ToArray());
            SuspendLayout();
            try
            {
                for (var i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    var rect = rectangles[i];
                    child.SetBounds(rect.X + child.Margin.Left, rect.Y + child.Margin.Top,
                        Math.Max(0, rect.Width - child.Margin.Horizontal),
                        Math.Max(0, rect.Height - child.Margin.Vertical));
                }
            }
            finally { ResumeLayout(false); }
        }
        finally { _arranging = false; }
    }
}
