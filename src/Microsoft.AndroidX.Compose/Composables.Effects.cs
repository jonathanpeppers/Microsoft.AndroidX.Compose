namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Registers an effect after each successful composition.</summary>
    public static void SideEffect(Action effect) =>
        ComposeExtensions.SideEffect(ComposableContext.Current, effect);

    /// <summary>Remembers a composition-owned coroutine scope.</summary>
    public static CoroutineScope RememberCoroutineScope() =>
        ComposeExtensions.RememberCoroutineScope(ComposableContext.Current);

    /// <summary>Registers a one-key disposable effect.</summary>
    public static void DisposableEffect(
        object? key1,
        Func<Action> effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ComposeExtensions.DisposableEffect(
            ComposableContext.Current, key1, effect);
    }

    /// <summary>Registers a two-key disposable effect.</summary>
    public static void DisposableEffect(
        object? key1,
        object? key2,
        Func<Action> effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ComposeExtensions.DisposableEffect(
            ComposableContext.Current, key1, key2, effect);
    }

    /// <summary>Registers a three-key disposable effect.</summary>
    public static void DisposableEffect(
        object? key1,
        object? key2,
        object? key3,
        Func<Action> effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        ComposeExtensions.DisposableEffect(
            ComposableContext.Current, key1, key2, key3, effect);
    }

    /// <summary>Registers a one-key launched effect.</summary>
    public static void LaunchedEffect(
        object? key1,
        Func<CancellationToken, Task> body) =>
        ComposeExtensions.LaunchedEffect(
            ComposableContext.Current, key1, body);

    /// <summary>Registers a two-key launched effect.</summary>
    public static void LaunchedEffect(
        object? key1,
        object? key2,
        Func<CancellationToken, Task> body) =>
        ComposeExtensions.LaunchedEffect(
            ComposableContext.Current, key1, key2, body);

    /// <summary>Registers a three-key launched effect.</summary>
    public static void LaunchedEffect(
        object? key1,
        object? key2,
        object? key3,
        Func<CancellationToken, Task> body) =>
        ComposeExtensions.LaunchedEffect(
            ComposableContext.Current, key1, key2, key3, body);
}
