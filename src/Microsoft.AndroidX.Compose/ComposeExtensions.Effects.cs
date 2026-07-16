using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>
    /// Compose's <c>SideEffect { … }</c>: runs <paramref name="effect"/>
    /// on every successful recomposition, <b>after</b> the composition
    /// has been applied. Use it to publish managed-side state into
    /// objects that aren't managed by Compose (e.g. logging, analytics,
    /// pushing a value into a non-Compose Android API).
    /// </summary>
    public static void SideEffect(this IComposer composer, Action effect)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(effect);
        EffectsKt.SideEffect(new ComposableLambda0(effect), composer, _changed: 0);
    }

    /// <summary>
    /// Remembers a composition-owned <see cref="CoroutineScope"/> that can
    /// launch asynchronous work from non-composable callbacks such as button
    /// clicks. The scope is cancelled automatically when this call leaves the
    /// composition.
    /// </summary>
    /// <remarks>
    /// Call <see cref="CoroutineScope.Launch"/> from an event handler and pass
    /// its cancellation token to Compose <c>*Async</c> APIs. Do not cache the
    /// returned scope outside the composition that created it.
    /// </remarks>
    public static CoroutineScope RememberCoroutineScope(this IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);

        // Kotlin source parameters are (getContext), followed by Composer,
        // $changed, and $default. Bit 0 asks Kotlin to use EmptyCoroutineContext.
        var scope = EffectsKt.RememberCoroutineScope(null, composer, 0, 1)
            ?? throw new InvalidOperationException(
                "rememberCoroutineScope did not return a Kotlin CoroutineScope.");
        return new CoroutineScope(scope);
    }

    /// <summary>
    /// Compose's <c>DisposableEffect(key1) { … onDispose { … } }</c>:
    /// runs <paramref name="effect"/> the first time this call site
    /// is composed (and again whenever <paramref name="key1"/> changes),
    /// and calls the returned cleanup <see cref="Action"/> on
    /// key change or when the call site leaves the composition.
    /// </summary>
    public static void DisposableEffect(
        this IComposer composer,
        object? key1,
        Func<DisposableEffectScope, Action> effect)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(effect);
        EffectsKt.DisposableEffect(
            ComposeBridges.BoxKey(key1),
            new DisposableEffectBody(effect),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Two-key overload of
    /// <see cref="DisposableEffect(IComposer, object?, Func{DisposableEffectScope, Action})"/>.
    /// </summary>
    public static void DisposableEffect(
        this IComposer composer,
        object? key1,
        object? key2,
        Func<DisposableEffectScope, Action> effect)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(effect);
        EffectsKt.DisposableEffect(
            ComposeBridges.BoxKey(key1),
            ComposeBridges.BoxKey(key2),
            new DisposableEffectBody(effect),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Three-key overload of
    /// <see cref="DisposableEffect(IComposer, object?, Func{DisposableEffectScope, Action})"/>.
    /// </summary>
    public static void DisposableEffect(
        this IComposer composer,
        object? key1,
        object? key2,
        object? key3,
        Func<DisposableEffectScope, Action> effect)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(effect);
        EffectsKt.DisposableEffect(
            ComposeBridges.BoxKey(key1),
            ComposeBridges.BoxKey(key2),
            ComposeBridges.BoxKey(key3),
            new DisposableEffectBody(effect),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Compose's <c>LaunchedEffect(key1) { … }</c>: launches the C#
    /// <paramref name="body"/> as a coroutine the first time this call
    /// site is composed (and again whenever <paramref name="key1"/>
    /// changes). The previous launch is cancelled on key change or
    /// when the call site leaves the composition.
    /// </summary>
    public static void LaunchedEffect(
        this IComposer composer,
        object? key1,
        Func<CancellationToken, Task> body)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(body);
        EffectsKt.LaunchedEffect(
            ComposeBridges.BoxKey(key1),
            new LaunchedEffectBody(body),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Two-key overload of
    /// <see cref="LaunchedEffect(IComposer, object?, Func{CancellationToken, Task})"/>.
    /// </summary>
    public static void LaunchedEffect(
        this IComposer composer,
        object? key1,
        object? key2,
        Func<CancellationToken, Task> body)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(body);
        EffectsKt.LaunchedEffect(
            ComposeBridges.BoxKey(key1),
            ComposeBridges.BoxKey(key2),
            new LaunchedEffectBody(body),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Three-key overload of
    /// <see cref="LaunchedEffect(IComposer, object?, Func{CancellationToken, Task})"/>.
    /// </summary>
    public static void LaunchedEffect(
        this IComposer composer,
        object? key1,
        object? key2,
        object? key3,
        Func<CancellationToken, Task> body)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(body);
        EffectsKt.LaunchedEffect(
            ComposeBridges.BoxKey(key1),
            ComposeBridges.BoxKey(key2),
            ComposeBridges.BoxKey(key3),
            new LaunchedEffectBody(body),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Bridges Compose's <c>snapshotFlow { producer() }</c> into an
    /// <see cref="IAsyncEnumerable{T}"/>. Every time any
    /// <see cref="MutableState{T}"/> / <see cref="MutableNumberState{T}"/>
    /// read inside <paramref name="producer"/> is written to and the
    /// surrounding snapshot is applied, the producer re-runs on Compose's
    /// main dispatcher and the new value flows through the returned
    /// async sequence.
    /// </summary>
    public static IAsyncEnumerable<T> SnapshotFlow<T>(Func<T> producer)
    {
        ArgumentNullException.ThrowIfNull(producer);
        return new SnapshotFlowEnumerable<T>(producer);
    }

    /// <summary>
    /// Convenience factory for a remembered
    /// <see cref="MutableManagedState{T}"/> seeded with
    /// <paramref name="initialValue"/>. The state is cached for the
    /// life of the composition slot, just like
    /// <see cref="Remember{T}(IComposer, Func{T}, int, string)"/>.
    /// </summary>
    public static MutableManagedState<T> MutableManagedStateOf<T>(
        this IComposer composer,
        T initialValue,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => composer.Remember(() => new MutableManagedState<T>(initialValue), line, file);
}
