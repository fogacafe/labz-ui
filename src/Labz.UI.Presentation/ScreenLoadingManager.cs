namespace Labz.UI.Presentation;

/// <summary>Screen-local reference-counted loading operations.</summary>
public interface IScreenLoadingManager
{
    bool IsLoading { get; }
    int ActiveCount { get; }
    string? Message { get; }
    event EventHandler? Changed;
    IDisposable Begin(string? message = null);
}

/// <summary>
/// A synchronous, thread-safe loading tracker. Changed fires on the calling thread;
/// WinForms consumers must marshal to their UI thread.
/// </summary>
public sealed class ScreenLoadingManager : IScreenLoadingManager, IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<long, string?> _operations = new();
    private long _nextId;
    private bool _disposed;

    public event EventHandler? Changed;

    public bool IsLoading { get { lock (_gate) return _operations.Count > 0; } }
    public int ActiveCount { get { lock (_gate) return _operations.Count; } }
    public string? Message
    {
        get
        {
            lock (_gate)
                return _operations.Count == 0 ? null : _operations.Last().Value;
        }
    }

    public IDisposable Begin(string? message = null)
    {
        long id;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            id = ++_nextId;
            _operations.Add(id, message);
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return new LoadingToken(this, id);
    }

    private void End(long id)
    {
        bool changed;
        lock (_gate)
        {
            if (_disposed) return;
            changed = _operations.Remove(id);
        }
        if (changed) Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        bool changed;
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            changed = _operations.Count != 0;
            _operations.Clear();
            Changed = null;
        }
        // No callbacks after disposal; UI hosts should unsubscribe during screen close.
        _ = changed;
    }

    private sealed class LoadingToken(ScreenLoadingManager owner, long id) : IDisposable
    {
        private ScreenLoadingManager? _owner = owner;
        public void Dispose() => Interlocked.Exchange(ref _owner, null)?.End(id);
    }
}
