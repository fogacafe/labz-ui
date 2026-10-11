namespace Labz.UI.WinForms.Navigation;

/// <summary>A module shown in the activity bar.</summary>
public sealed record LabzModule(string Id, string Title, string Icon = "")
{
    public List<LabzNavigationPage> Pages { get; } = [];
}

/// <summary>A page entry; Group is used to organize sidebar entries.</summary>
public sealed record LabzNavigationPage(string Id, string Title, string Group = "Pages");
