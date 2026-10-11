namespace Labz.UI.Presentation;

/// <summary>Screen-local typed events. Synchronous handlers run on the publishing thread.</summary>
public interface IScreenEventBus
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    void Publish<TEvent>(TEvent message);
}

/// <summary>
/// Scoped event bus with disposable subscriptions. The bus must not outlive its screen scope.
/// UI subscribers must publish/marshal on the WinForms UI thread.
/// </summary>
public sealed class ScreenEventBus : IScreenEventBus, IDisposable
{
    private readonly object _gate = new();
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private bool _disposed;

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var key = typeof(TEvent);
            if (!_handlers.TryGetValue(key, out var listeners))
                _handlers[key] = listeners = new List<Delegate>();
            listeners.Add(handler);
        }
        return new Subscription(() => Unsubscribe(handler));
    }

    public void Publish<TEvent>(TEvent message)
    {
        Delegate[] listeners;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            listeners = _handlers.TryGetValue(typeof(TEvent), out var found)
                ? found.ToArray()
                : Array.Empty<Delegate>();
        }
        foreach (var listener in listeners)
            ((Action<TEvent>)listener)(message);
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        lock (_gate)
        {
            if (_disposed) return;
            if (!_handlers.TryGetValue(typeof(TEvent), out var listeners)) return;
            listeners.Remove(handler);
            if (listeners.Count == 0) _handlers.Remove(typeof(TEvent));
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _handlers.Clear();
        }
    }

    private sealed class Subscription(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
