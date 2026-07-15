using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Produces state for the lifetime of the current composition.</summary>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.ProduceState(
            ComposableContext.Current, initialValue, producer, line, file);

    /// <summary>Produces state and restarts when <paramref name="key1"/> changes.</summary>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        object? key1,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.ProduceState(
            ComposableContext.Current, initialValue, key1, producer, line, file);

    /// <summary>Produces state and restarts when either key changes.</summary>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        object? key1,
        object? key2,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.ProduceState(
            ComposableContext.Current, initialValue, key1, key2, producer, line, file);

    /// <summary>Produces state and restarts when any key changes.</summary>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        object? key1,
        object? key2,
        object? key3,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.ProduceState(
            ComposableContext.Current, initialValue, key1, key2, key3, producer, line, file);

    /// <summary>Produces state using an array of restart keys.</summary>
    public static MutableState<T> ProduceStateKeyed<T>(
        T initialValue,
        object?[] keys,
        Func<MutableState<T>, CancellationToken, Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.ProduceStateKeyed(
            ComposableContext.Current, initialValue, keys, producer, line, file);
}
