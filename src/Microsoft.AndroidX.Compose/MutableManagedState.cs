namespace AndroidX.Compose;

/// <summary>
/// Thread-safe managed state whose reads participate in Compose dependency
/// tracking. Designed for view models that need to hold arbitrary CLR values
/// without converting them to Java objects.
/// </summary>
/// <typeparam name="T">The managed value type.</typeparam>
/// <remarks>
/// <para>
/// The value is held in a managed field and paired with an internal Compose
/// state version. Reading <see cref="Value"/> inside composition subscribes
/// the surrounding scope; changing the value increments the version and
/// invalidates those readers. Equal values, according to
/// <see cref="EqualityComparer{T}.Default"/>, do not cause invalidation.
/// </para>
/// <para>
/// This is not Kotlin <c>StateFlow</c>, an <see cref="IObservable{T}"/>, or a
/// full Compose snapshot container. It has no collection, replay, subscriber,
/// or lifecycle behavior. Use the
/// <see cref="ComposeExtensions.CollectAsStateWithLifecycle{T}(Xamarin.KotlinX.Coroutines.Flow.IStateFlow, AndroidX.Compose.Runtime.IComposer)"/>
/// bridge for a real Kotlin <c>StateFlow</c>.
/// </para>
/// <para>
/// Reads and writes are synchronized. <see cref="Update"/> runs its transform
/// while holding the instance lock, so concurrent updates observe one another
/// without lost writes. Keep transforms short and side-effect free.
/// </para>
/// </remarks>
public sealed class MutableManagedState<T> : IState<T>
{
    readonly object _lock = new();
    readonly MutableNumberState<int> _tick = new(0);
    T _value;

    /// <summary>Creates managed state initialized with <paramref name="initialValue"/>.</summary>
    public MutableManagedState(T initialValue) => _value = initialValue;

    /// <summary>
    /// Gets or sets the current value. Reads inside composition subscribe the
    /// surrounding scope; a distinct write invalidates subscribed scopes.
    /// </summary>
    public T Value
    {
        get
        {
            lock (_lock)
            {
                _ = _tick.Value;
                return _value;
            }
        }
        set => TrySet(value);
    }

    /// <summary>
    /// Sets <see cref="Value"/> to <paramref name="value"/> and returns
    /// <c>true</c> when the value changed, or <c>false</c> when it was equal
    /// to the current value.
    /// </summary>
    public bool TrySet(T value)
    {
        lock (_lock)
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
                return false;
            _value = value;
            _tick.Value++;
            return true;
        }
    }

    /// <summary>
    /// Atomically applies <paramref name="transform"/> to the current value
    /// and returns the resulting value.
    /// </summary>
    /// <remarks>
    /// The transform runs under the instance lock. If it returns a value equal
    /// to the current value, no invalidation occurs.
    /// </remarks>
    public T Update(Func<T, T> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        lock (_lock)
        {
            var next = transform(_value);
            if (EqualityComparer<T>.Default.Equals(_value, next))
                return _value;
            _value = next;
            _tick.Value++;
            return next;
        }
    }
}
