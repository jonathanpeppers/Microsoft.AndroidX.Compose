using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Gets or creates a composition-owned view model.</summary>
    public static T ViewModel<T>(
        Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        where T : ViewModel =>
        ComposeExtensions.ViewModel(
            ComposableContext.Current, factory, line, file);

    /// <summary>Gets or creates a view model identified by <paramref name="key1"/>.</summary>
    public static T ViewModel<T>(
        Func<T> factory,
        object? key1,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        where T : ViewModel =>
        ComposeExtensions.ViewModel(
            ComposableContext.Current, factory, key1, line, file);

    /// <summary>Gets or creates a view model identified by two keys.</summary>
    public static T ViewModel<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        where T : ViewModel =>
        ComposeExtensions.ViewModel(
            ComposableContext.Current, factory, key1, key2, line, file);

    /// <summary>Gets or creates a view model identified by three keys.</summary>
    public static T ViewModel<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        object? key3,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        where T : ViewModel =>
        ComposeExtensions.ViewModel(
            ComposableContext.Current, factory, key1, key2, key3, line, file);

    /// <summary>Gets or creates a view model identified by an array of keys.</summary>
    public static T ViewModelKeyed<T>(
        Func<T> factory,
        object?[] keys,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        where T : ViewModel =>
        ComposeExtensions.ViewModelKeyed(
            ComposableContext.Current, factory, keys, line, file);
}
