using System.Collections.ObjectModel;

namespace Labz.UI.WinForms.Navigation;

/// <summary>Presentation-only user identity supplied by the host application.</summary>
public sealed record LabzUserContext
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Email { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>Presentation-only tenant identity supplied by the host application.</summary>
public sealed record LabzTenantContext
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
}

/// <summary>Context captured at the moment a footer action is requested.</summary>
public sealed class LabzContextRequestedEventArgs<TContext>(TContext? context) : EventArgs
    where TContext : class
{
    public TContext? Context { get; } = context;
}
