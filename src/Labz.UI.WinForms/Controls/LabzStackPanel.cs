using System.ComponentModel;
using Labz.UI.WinForms.Layout;

namespace Labz.UI.WinForms.Controls;

/// <summary>Designer-compatible container with CSS-inspired directional layout.</summary>
[DefaultProperty(nameof(Direction))]
public class LabzStackPanel : Panel
{
    private LabzDirection _direction = LabzDirection.Vertical;
    private LabzJustifyContent _justifyContent;
    private LabzAlignItems _alignItems;
    private int _spacing = 8;
    private bool _wrap;
    private bool _arranging;
    private readonly Dictionary<Control, (double Grow, double Shrink, Size Basis)> _flex = [];

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

    /// <summary>Sets per-child flex factors; default is grow=0, shrink=1.</summary>
    public void SetFlex(Control child, double grow = 0, double shrink = 1)
    {
        ArgumentNullException.ThrowIfNull(child);
        if (!double.IsFinite(grow) || grow < 0 || !double.IsFinite(shrink) || shrink < 0)
            throw new ArgumentOutOfRangeException(nameof(grow));
        if (child.Parent != this) throw new ArgumentException("Control must belong to this panel.", nameof(child));
        _flex[child] = (grow, shrink, child.Size);
        PerformLayout();
    }

    protected override void OnControlRemoved(ControlEventArgs e)
    {
        base.OnControlRemoved(e);
        _flex.Remove(e.Control);
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
                var basis = _flex.TryGetValue(c, out var factors) ? factors.Basis : c.Size;
                return new Size(basis.Width + c.Margin.Horizontal, basis.Height + c.Margin.Vertical);
            }).ToArray();
            var bounds = DisplayRectangle;
            var rectangles = Wrap
                ? LabzFlexLayoutEngine.Arrange(sizes, bounds, Direction,
                    Spacing, true, JustifyContent, AlignItems)
                : LabzFlexLayoutEngine.ArrangeFlexible(sizes, bounds, Direction,
                    Spacing, JustifyContent, AlignItems,
                    children.Select(c => _flex.TryGetValue(c, out var f) ? f.Grow : 0d).ToArray(),
                    children.Select(c => _flex.TryGetValue(c, out var f) ? f.Shrink : 1d).ToArray());
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
