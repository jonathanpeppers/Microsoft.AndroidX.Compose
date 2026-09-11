using Android.Runtime;
using Kotlin.Jvm.Functions;

namespace AndroidX.Compose;

/// <summary>Value-returning Function1 for lazy collection and pager item identity.</summary>
/// <remarks>
/// Unlike action callbacks, this adapter returns a Bundle-saveable Java value.
/// Keys and items are snapshotted together so deferred measurement cannot pair
/// an old key with a newly inserted item. No custom JNI bridge is needed.
/// </remarks>
[Register("net/compose/CollectionItemKey")]
internal sealed class CollectionItemKey : Java.Lang.Object, IFunction1
{
    readonly object[] _keys;

    CollectionItemKey(object[] keys) => _keys = keys;

    internal static (IReadOnlyList<T> Items, CollectionItemKey? Key) Create<T>(
        IReadOnlyList<T> items, Func<T, object>? key)
    {
        if (key is null)
            return (items, null);

        T[] snapshot = [.. items];
        var keys = new object[snapshot.Length];
        var indices = new Dictionary<object, int>();
        for (int i = 0; i < snapshot.Length; i++)
        {
            object value = key(snapshot[i]);
            if (value is not (string or int or long))
                throw new ArgumentException(
                    $"Item key at index {i} must be a non-null string, int, or long; " +
                    $"got {value?.GetType().FullName ?? "null"}.", nameof(key));
            if (!indices.TryAdd(value, i))
                throw new ArgumentException(
                    $"Duplicate item key at indices {indices[value]} and {i}. " +
                    "Keys must be unique within the collection.", nameof(key));
            keys[i] = value;
        }
        return (snapshot, new CollectionItemKey(keys));
    }

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        if (p0 is not Java.Lang.Integer index)
            throw new ArgumentException("Item key callback requires a boxed integer index.", nameof(p0));
        int i = index.IntValue();
        if ((uint)i >= (uint)_keys.Length)
            throw new ArgumentOutOfRangeException(nameof(p0), i, "Item key index is outside the collection.");
        return _keys[i] switch
        {
            string value => new Java.Lang.String(value),
            int value => Java.Lang.Integer.ValueOf(value),
            long value => Java.Lang.Long.ValueOf(value),
            _ => throw new InvalidOperationException("Collection item keys were not validated."),
        };
    }
}
