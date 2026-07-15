using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>
    /// Remembers a value at the current implicit-composer call site.
    /// </summary>
    public static T Remember<T>(
        Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.Remember(ComposableContext.Current, factory, line, file);

    /// <summary>Remembers a value until <paramref name="key1"/> changes.</summary>
    public static T Remember<T>(
        Func<T> factory,
        object? key1,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.Remember(
            ComposableContext.Current, factory, key1, line, file);

    /// <summary>Remembers a value until either key changes.</summary>
    public static T Remember<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.Remember(
            ComposableContext.Current, factory, key1, key2, line, file);

    /// <summary>Remembers a value until any key changes.</summary>
    public static T Remember<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        object? key3,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.Remember(
            ComposableContext.Current, factory, key1, key2, key3, line, file);

    /// <summary>Remembers a value using an array of keys.</summary>
    public static T RememberKeyed<T>(
        Func<T> factory,
        object?[] keys,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberKeyed(
            ComposableContext.Current, factory, keys, line, file);

    /// <summary>Remembers and saves a value across activity recreation.</summary>
    public static T RememberSaveable<T>(
        Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberSaveable(
            ComposableContext.Current, factory, line, file);

    /// <summary>Remembers and saves a value until <paramref name="key1"/> changes.</summary>
    public static T RememberSaveable<T>(
        Func<T> factory,
        object? key1,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberSaveable(
            ComposableContext.Current, factory, key1, line, file);

    /// <summary>Remembers and saves a value until either key changes.</summary>
    public static T RememberSaveable<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberSaveable(
            ComposableContext.Current, factory, key1, key2, line, file);

    /// <summary>Remembers and saves a value until any key changes.</summary>
    public static T RememberSaveable<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        object? key3,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberSaveable(
            ComposableContext.Current, factory, key1, key2, key3, line, file);

    /// <summary>Remembers and saves a value using an array of keys.</summary>
    public static T RememberSaveableKeyed<T>(
        Func<T> factory,
        object?[] keys,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberSaveableKeyed(
            ComposableContext.Current, factory, keys, line, file);
}
