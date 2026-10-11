using System.Windows.Forms;

namespace Labz.UI.Presentation;

/// <summary>Controls whether opening a screen creates a new instance or selects an existing keyed instance.</summary>
public enum ScreenOpenPolicy { NewInstance, ReuseByKey }

/// <summary>
/// Small navigation registry over the existing deferred factory.
/// One navigator belongs to one tab host and must be used on its WinForms UI thread.
/// The host still owns visual tab attachment and closing.
/// </summary>
public sealed class ScreenNavigator(DeferredScreenFactory factory)
{
    private readonly Dictionary<(Type View, string Key), object> _keyed = new();

    public DeferredScreenInstance<TView> Open<TView, TPresenter>(
        ScreenOpenPolicy policy = ScreenOpenPolicy.NewInstance,
        string? key = null,
        CancellationToken cancellationToken = default)
        where TView : Form
        where TPresenter : class, IAsyncScreenPresenter<TView>
    {
        if (policy == ScreenOpenPolicy.ReuseByKey && string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("ReuseByKey requires a non-empty key.", nameof(key));

        cancellationToken.ThrowIfCancellationRequested();
        if (policy == ScreenOpenPolicy.ReuseByKey)
        {
            var id = (typeof(TView), key!);
            if (_keyed.TryGetValue(id, out var existing))
            {
                var screen = (DeferredScreenInstance<TView>)existing;
                if (!screen.View.IsDisposed) return screen;
                _keyed.Remove(id);
            }
        }

        var created = factory.Open<TView, TPresenter>(cancellationToken);
        if (policy == ScreenOpenPolicy.ReuseByKey)
            _keyed.Add((typeof(TView), key!), created);
        return created;
    }

    /// <summary>Forget an instance after the host has removed and disposed its tab.</summary>
    public void Forget<TView>(DeferredScreenInstance<TView> instance) where TView : Form
    {
        ArgumentNullException.ThrowIfNull(instance);
        foreach (var entry in _keyed.Where(x => ReferenceEquals(x.Value, instance)).Select(x => x.Key).ToArray())
            _keyed.Remove(entry);
    }
}
