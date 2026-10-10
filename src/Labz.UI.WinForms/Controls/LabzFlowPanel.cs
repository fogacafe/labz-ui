using System.ComponentModel;
using Labz.UI.WinForms.Layout;

namespace Labz.UI.WinForms.Controls;

/// <summary>Wrap-first layout container; shares the same layout engine as LabzStackPanel.</summary>
public class LabzFlowPanel : LabzStackPanel
{
    public LabzFlowPanel()
    {
        Direction = LabzDirection.Horizontal;
        Wrap = true;
    }

    [Category("Layout")]
    [DefaultValue(LabzDirection.Horizontal)]
    public new LabzDirection Direction
    {
        get => base.Direction;
        set => base.Direction = value;
    }

    [Category("Layout")]
    [DefaultValue(true)]
    public new bool Wrap
    {
        get => base.Wrap;
        set => base.Wrap = value;
    }
}
