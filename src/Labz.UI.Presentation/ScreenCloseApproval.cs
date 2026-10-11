namespace Labz.UI.Presentation;

/// <summary>
/// Runs close approvals sequentially without disposing any screen.
/// The caller owns synchronization and cleanup after a successful preflight.
/// </summary>
public static class ScreenCloseApproval
{
    public static async Task<bool> ApproveAllAsync<T>(
        IReadOnlyList<T> screens,
        Func<T, CancellationToken, ValueTask<bool>>? guard,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(screens);
        foreach (var screen in screens)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (guard is not null && !await guard(screen, cancellationToken))
                return false;
        }
        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }
}
