using System.ComponentModel;
using Labz.UI.Core.Theming;

namespace Labz.UI.WinForms.Controls;

/// <summary>Theme-aware content card with optional heading and border.</summary>
public class LabzCard : LabzPanel
{
    private string _title = "";
    private readonly LabzLabel _heading = new()
    {
        Dock = DockStyle.Top, Variant = LabzLabelVariant.Heading,
        AutoSize = false, Height = 36, Padding = new Padding(0, 0, 0, 8)
    };

    public LabzCard()
    {
        Padding = new Padding(16);
        Controls.Add(_heading);
        _heading.Visible = false;
    }

    [Category("Appearance")]
    [DefaultValue("")]
    public string Title
    {
        get => _title;
        set
        {
            _title = value ?? "";
            _heading.Text = _title;
            _heading.Visible = !string.IsNullOrWhiteSpace(_title);
        }
    }
}
