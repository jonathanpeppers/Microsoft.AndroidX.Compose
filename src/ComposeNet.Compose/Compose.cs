using System.Runtime.CompilerServices;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Top-level composition utilities that need to be callable from anywhere
/// inside a composition pass — not just from <see cref="ComposeActivity"/>
/// subclasses. Use these from extracted helper composables, custom
/// <see cref="ComposableNode"/> subclasses, etc.
/// </summary>
public static class Compose
{
    /// <summary>
    /// Compose's <c>remember { factory() }</c>, backed by the active
    /// composer's slot table. Returns the value of <paramref name="factory"/>
    /// the first time this call site is reached, then the cached value on
    /// subsequent recompositions.
    ///
    /// The slot is keyed by <see cref="SourceLocationKey"/>'s FNV-1a hash
    /// of the file path mixed with the line number — a stable identifier
    /// derived from <see cref="CallerLineNumberAttribute"/> /
    /// <see cref="CallerFilePathAttribute"/> fill-ins, deterministic
    /// across process restarts (unlike <see cref="System.HashCode"/>,
    /// which is per-process randomized) so the saveable-state registry
    /// can match the recomputed key to its stored value on restore.
    /// Two call sites in different files (or different lines) never
    /// share a slot, and the same call site reached repeatedly across
    /// recompositions does share its slot.
    ///
    /// Wrapped in <c>StartReplaceableGroup</c> / <c>EndReplaceableGroup</c>
    /// so the slot belongs to its own group; sibling positional grouping
    /// (see <see cref="ComposableContainer.RenderChildren"/>) then keeps
    /// repeated helper calls in a parent container from sharing slots.
    ///
    /// Must be called inside a composition (i.e. on the thread currently
    /// running a <see cref="ComposableLambda2"/>/<c>3</c>/<c>4</c> body, or
    /// inside <c>Render</c> on a node reached from one of those). Otherwise
    /// throws <see cref="System.InvalidOperationException"/>.
    /// </summary>
    public static T Remember<T>(
        System.Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberCore(factory, keys: null, line, file);

    /// <summary>
    /// Keyed <c>remember(key1) { factory() }</c>: the cached value is
    /// re-created whenever <paramref name="key1"/> changes (structural
    /// equality via <see cref="object.Equals(object?, object?)"/>).
    /// Use to recompute derived values when their inputs change without
    /// having to manually clear / rebuild state on every recomposition.
    /// </summary>
    public static T Remember<T>(
        System.Func<T> factory,
        object? key1,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberCore(factory, new[] { key1 }, line, file);

    /// <summary>Keyed <c>remember(key1, key2) { factory() }</c>.</summary>
    public static T Remember<T>(
        System.Func<T> factory,
        object? key1,
        object? key2,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberCore(factory, new[] { key1, key2 }, line, file);

    /// <summary>Keyed <c>remember(key1, key2, key3) { factory() }</c>.</summary>
    public static T Remember<T>(
        System.Func<T> factory,
        object? key1,
        object? key2,
        object? key3,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberCore(factory, new[] { key1, key2, key3 }, line, file);

    /// <summary>
    /// Array-form keyed <c>remember(vararg keys) { factory() }</c>.
    /// Use when the caller has more than three keys or already has the
    /// keys in an array. The array is cloned defensively so subsequent
    /// caller mutation doesn't corrupt the "previous keys" comparison.
    /// </summary>
    public static T RememberKeyed<T>(
        System.Func<T> factory,
        object?[] keys,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberCore(factory, keys ?? throw new System.ArgumentNullException(nameof(keys)), line, file);

    static T RememberCore<T>(System.Func<T> factory, object?[]? keys, int line, string file)
    {
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.Remember<T> must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            if (composer.RememberedValue() is RememberHolder existing
                && RememberHolder.KeysEqual(existing.Keys, keys))
            {
                return (T)existing.Value!;
            }
            var value = factory();
            composer.UpdateRememberedValue(new RememberHolder(value, keys));
            return value;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    /// <summary>
    /// Compose's <c>rememberSaveable { factory() }</c>: like
    /// <see cref="Remember{T}(System.Func{T}, int, string)"/>, but the
    /// cached value also survives <b>process death and activity
    /// recreation</b> (e.g. rotation when the activity doesn't override
    /// <c>android:configChanges</c>) via Compose's
    /// <c>SaveableStateRegistry</c>, which serialises into the
    /// activity's saved-instance <see cref="Android.OS.Bundle"/>.
    ///
    /// Mirrors Kotlin's single <c>rememberSaveable&lt;T&gt;</c> entry
    /// point — the same call works for scalar values
    /// (<c>int</c>, <c>long</c>, <c>float</c>, <c>double</c>,
    /// <c>bool</c>, <c>string</c>, plus any other type
    /// <see cref="MutableState{T}"/> can box / unbox) and for
    /// state-holder wrappers (<see cref="MutableState{U}"/>,
    /// <see cref="MutableNumberState{U}"/>):
    /// <code>
    /// var count = RememberSaveable(() =&gt; new MutableNumberState&lt;int&gt;(0));
    /// var name  = RememberSaveable(() =&gt; new MutableState&lt;string&gt;(""));
    /// var pi    = RememberSaveable(() =&gt; 3.14159);
    /// </code>
    /// Wrappers are routed through Compose's <c>mutableStateSaver</c>
    /// (only the inner value is persisted); scalars use
    /// <c>autoSaver</c>.
    ///
    /// The slot is keyed by <see cref="SourceLocationKey"/>'s FNV-1a
    /// hash — deterministic across process restarts so the saveable-
    /// state registry can match the recomputed key to its stored value
    /// on restore.
    /// </summary>
    public static T RememberSaveable<T>(
        System.Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberSaveableCore(factory, keys: null, line, file);

    /// <summary>
    /// Keyed <c>rememberSaveable(key1) { factory() }</c>: keys flow into
    /// Kotlin's <c>inputs</c> array so Compose's saveable registry
    /// invalidates the cached value when any key changes — matching the
    /// in-memory behaviour of the keyed <see cref="Remember{T}(System.Func{T}, object?, int, string)"/>.
    /// </summary>
    public static T RememberSaveable<T>(
        System.Func<T> factory,
        object? key1,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberSaveableCore(factory, new[] { key1 }, line, file);

    /// <summary>Keyed <c>rememberSaveable(key1, key2) { factory() }</c>.</summary>
    public static T RememberSaveable<T>(
        System.Func<T> factory,
        object? key1,
        object? key2,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberSaveableCore(factory, new[] { key1, key2 }, line, file);

    /// <summary>Keyed <c>rememberSaveable(key1, key2, key3) { factory() }</c>.</summary>
    public static T RememberSaveable<T>(
        System.Func<T> factory,
        object? key1,
        object? key2,
        object? key3,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberSaveableCore(factory, new[] { key1, key2, key3 }, line, file);

    /// <summary>
    /// Array-form keyed <c>rememberSaveable(vararg inputs) { factory() }</c>.
    /// Use when the caller has more than three keys or already has the
    /// keys in an array.
    /// </summary>
    public static T RememberSaveableKeyed<T>(
        System.Func<T> factory,
        object?[] keys,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => RememberSaveableCore(factory, keys ?? throw new System.ArgumentNullException(nameof(keys)), line, file);

    static T RememberSaveableCore<T>(System.Func<T> factory, object?[]? keys, int line, string file)
    {
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.RememberSaveable<T> must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            // State-holder branch: T is MutableState<U>, MutableNumberState<U>,
            // or any future wrapper that implements IMutableStateWrapper.
            // The `is`-check is type-metadata only — no reflection, no
            // member lookup, fully trim-safe.
            if (typeof(IMutableStateWrapper).IsAssignableFrom(typeof(T)))
                return RememberSaveableWrapper(factory, keys, composer);

            return RememberSaveableScalar(factory, keys, composer);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    static T RememberSaveableScalar<T>(System.Func<T> factory, object?[]? keys, IComposer composer)
    {
        var inputs = ComposeBridges.BuildKeysArray(keys, out var ownsInputs);
        var jcw = new ObjectFunction0(() => MutableState<T>.ToJava(factory()));
        var handle = ComposeBridges.RememberSaveableSimple(
            inputs,
            jcw,
            ((Java.Lang.Object)composer).Handle,
            changed: 0);
        try
        {
            if (handle == System.IntPtr.Zero)
                return default!;
            var boxed = Java.Lang.Object.GetObject<Java.Lang.Object>(
                handle, Android.Runtime.JniHandleOwnership.DoNotTransfer);
            return MutableState<T>.FromJava(boxed);
        }
        finally
        {
            if (handle != System.IntPtr.Zero)
                Android.Runtime.JNIEnv.DeleteLocalRef(handle);
            if (ownsInputs && inputs != System.IntPtr.Zero)
                Android.Runtime.JNIEnv.DeleteLocalRef(inputs);
            System.GC.KeepAlive(composer);
        }
    }

    static T RememberSaveableWrapper<T>(System.Func<T> factory, object?[]? keys, IComposer composer)
    {
        // Cache the C# wrapper across recompositions so we don't
        // allocate a fresh facade + Kotlin IMutableState on every
        // render. `Compose.Remember` opens its own nested replaceable
        // group; nesting is fine — Compose's slot table handles it.
        // We use the keyless Remember on purpose: Compose's keyed
        // rememberSaveable below already invalidates on key change
        // and hands us a fresh IMutableState, which we then swap in.
        var wrapper = Remember(factory)
            ?? throw new System.InvalidOperationException(
                $"Compose.RememberSaveable<{typeof(T).Name}>: factory returned null.");
        var iwrap = (IMutableStateWrapper)wrapper;

        var inputs = ComposeBridges.BuildKeysArray(keys, out var ownsInputs);
        // Hand Compose's `rememberSaveable` the wrapper's underlying
        // IMutableState. On first composition Compose calls our JCW
        // and caches whatever we return. On a process-death restore
        // Compose builds a fresh boxed state via mutableStateSaver
        // and ignores our JCW; its return value is the restored
        // state. On every subsequent recomposition Compose returns
        // the cached state directly. We swap our wrapper's _state
        // to point at whatever Compose hands back.
        var jcw = new ObjectFunction0(() => (Java.Lang.Object)iwrap.State);
        var handle = ComposeBridges.RememberSaveableMutableState(
            inputs,
            ComposeBridges.SaverAutoSaver(),
            jcw,
            ((Java.Lang.Object)composer).Handle,
            changed: 0);
        try
        {
            if (handle == System.IntPtr.Zero)
                throw new System.InvalidOperationException(
                    $"Compose.RememberSaveable<{typeof(T).Name}>: rememberSaveable returned null.");
            iwrap.State = Java.Lang.Object.GetObject<IMutableState>(
                handle, Android.Runtime.JniHandleOwnership.DoNotTransfer)!;
            return wrapper;
        }
        finally
        {
            if (handle != System.IntPtr.Zero)
                Android.Runtime.JNIEnv.DeleteLocalRef(handle);
            if (ownsInputs && inputs != System.IntPtr.Zero)
                Android.Runtime.JNIEnv.DeleteLocalRef(inputs);
            System.GC.KeepAlive(composer);
        }
    }

    /// <summary>
    /// Compose's <c>rememberDraggableState(onDelta)</c>: build a
    /// <see cref="DraggableState"/> whose underlying Kotlin handle is
    /// cached in the active composer's slot table for the lifetime of
    /// this call site. The <paramref name="onDelta"/> callback is
    /// wrapped through Kotlin's <c>rememberUpdatedState</c>, so passing
    /// a fresh lambda each recomposition is safe — the Java
    /// <c>DraggableState</c> identity stays stable while the callback
    /// can capture changing recomposition-time state.
    ///
    /// Must be called inside a composition (e.g. inside a
    /// <c>SetContent</c> body or a <see cref="ComposableNode.Render(IComposer)"/>
    /// override). Pair with
    /// <see cref="Modifier.Draggable(DraggableState, ComposeNet.Orientation, bool)"/>
    /// — the returned state is the value to hand to that modifier.
    /// </summary>
    public static DraggableState RememberDraggableState(
        System.Action<float> onDelta,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        System.ArgumentNullException.ThrowIfNull(onDelta);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.RememberDraggableState must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            var jcw = new ComposableLambda1(boxed =>
            {
                var f = boxed as Java.Lang.Float
                    ?? throw new System.InvalidCastException(
                        $"Expected java.lang.Float in DraggableState.onDelta; got '{boxed?.Class?.Name ?? "null"}'.");
                onDelta(f.FloatValue());
            });
            var jvm = AndroidX.Compose.Foundation.Gestures.DraggableKt.RememberDraggableState(jcw, composer, 0)
                ?? throw new System.InvalidOperationException(
                    "DraggableKt.RememberDraggableState returned null.");
            return new DraggableState(jvm);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    /// <summary>
    /// Compose's <c>SideEffect { … }</c>: runs <paramref name="effect"/>
    /// on every successful recomposition, <b>after</b> the composition
    /// has been applied. Use it to publish managed-side state into
    /// objects that aren't managed by Compose (e.g. logging, analytics,
    /// pushing a value into a non-Compose Android API).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="effect"/> must <b>not</b> mutate any state that
    /// the same composition reads — that would invalidate the
    /// composition Compose just applied and trigger an infinite
    /// recomposition loop.
    /// </para>
    /// <para>
    /// Stale captures: closures inside <paramref name="effect"/> see
    /// whatever the C# closure captured during the most recent render
    /// — <c>SideEffect</c> bodies are recreated every render so this
    /// is generally what you want.
    /// </para>
    /// </remarks>
    public static void SideEffect(System.Action effect)
    {
        System.ArgumentNullException.ThrowIfNull(effect);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.SideEffect must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        EffectsKt.SideEffect(new ComposableLambda0(effect), composer, _changed: 0);
    }

    /// <summary>
    /// Compose's <c>DisposableEffect(key1) { … onDispose { … } }</c>:
    /// runs <paramref name="effect"/> the first time this call site
    /// is composed (and again whenever <paramref name="key1"/> changes),
    /// and calls the returned cleanup <see cref="System.Action"/> on
    /// key change or when the call site leaves the composition.
    /// </summary>
    /// <param name="key1">
    /// Compose compares this against the previous value using
    /// <c>Object.equals</c> via the boxed Java value. Supports
    /// <c>null</c>, any <see cref="Java.Lang.Object"/> peer,
    /// <see cref="string"/>, and every common .NET primitive.
    /// </param>
    /// <param name="effect">
    /// Setup callback. Must return a non-null cleanup
    /// <see cref="System.Action"/> — use <c>() =&gt; { }</c> when
    /// there's nothing to clean up.
    /// </param>
    /// <remarks>
    /// Stale captures: closures inside <paramref name="effect"/> see
    /// whatever the C# closure captured when the effect was last
    /// set up. To observe newer values without invalidating the
    /// effect, hoist them into <see cref="MutableState{T}"/>.
    /// </remarks>
    public static void DisposableEffect(
        object? key1,
        System.Func<AndroidX.Compose.Runtime.DisposableEffectScope, System.Action> effect)
    {
        System.ArgumentNullException.ThrowIfNull(effect);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.DisposableEffect must be called inside a composition.");

        EffectsKt.DisposableEffect(
            ComposeBridges.BoxKey(key1),
            new DisposableEffectBody(effect),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Two-key overload of
    /// <see cref="DisposableEffect(object?, System.Func{AndroidX.Compose.Runtime.DisposableEffectScope, System.Action})"/>.
    /// </summary>
    public static void DisposableEffect(
        object? key1,
        object? key2,
        System.Func<AndroidX.Compose.Runtime.DisposableEffectScope, System.Action> effect)
    {
        System.ArgumentNullException.ThrowIfNull(effect);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.DisposableEffect must be called inside a composition.");

        EffectsKt.DisposableEffect(
            ComposeBridges.BoxKey(key1),
            ComposeBridges.BoxKey(key2),
            new DisposableEffectBody(effect),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Three-key overload of
    /// <see cref="DisposableEffect(object?, System.Func{AndroidX.Compose.Runtime.DisposableEffectScope, System.Action})"/>.
    /// </summary>
    public static void DisposableEffect(
        object? key1,
        object? key2,
        object? key3,
        System.Func<AndroidX.Compose.Runtime.DisposableEffectScope, System.Action> effect)
    {
        System.ArgumentNullException.ThrowIfNull(effect);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.DisposableEffect must be called inside a composition.");

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
    /// when the call site leaves the composition — the
    /// <see cref="System.Threading.CancellationToken"/> passed to
    /// <paramref name="body"/> is signalled, and the body should
    /// observe it (e.g. via <see cref="System.Threading.Tasks.Task.Delay(int, System.Threading.CancellationToken)"/>).
    /// </summary>
    /// <param name="key1">
    /// Compose compares this against the previous value using
    /// <c>Object.equals</c> via the boxed Java value. Pass a stable
    /// "version" (e.g. <see cref="System.Guid.Empty"/> stringified, or
    /// the literal <c>"once"</c>) when you want the body to run
    /// exactly once per call-site lifetime.
    /// </param>
    /// <param name="body">
    /// The async work to run. Honours the supplied
    /// <see cref="System.Threading.CancellationToken"/> when the
    /// underlying Kotlin <c>Job</c> is cancelled.
    /// </param>
    /// <remarks>
    /// <para>
    /// Stale captures: closures inside <paramref name="body"/> see
    /// whatever the C# closure captured when the launch started. To
    /// observe newer values without restarting the body, read from
    /// <see cref="MutableState{T}"/>.
    /// </para>
    /// </remarks>
    public static void LaunchedEffect(
        object? key1,
        System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> body)
    {
        System.ArgumentNullException.ThrowIfNull(body);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.LaunchedEffect must be called inside a composition.");

        EffectsKt.LaunchedEffect(
            ComposeBridges.BoxKey(key1),
            new LaunchedEffectBody(body),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Two-key overload of
    /// <see cref="LaunchedEffect(object?, System.Func{System.Threading.CancellationToken, System.Threading.Tasks.Task})"/>.
    /// </summary>
    public static void LaunchedEffect(
        object? key1,
        object? key2,
        System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> body)
    {
        System.ArgumentNullException.ThrowIfNull(body);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.LaunchedEffect must be called inside a composition.");

        EffectsKt.LaunchedEffect(
            ComposeBridges.BoxKey(key1),
            ComposeBridges.BoxKey(key2),
            new LaunchedEffectBody(body),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Three-key overload of
    /// <see cref="LaunchedEffect(object?, System.Func{System.Threading.CancellationToken, System.Threading.Tasks.Task})"/>.
    /// </summary>
    public static void LaunchedEffect(
        object? key1,
        object? key2,
        object? key3,
        System.Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> body)
    {
        System.ArgumentNullException.ThrowIfNull(body);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.LaunchedEffect must be called inside a composition.");

        EffectsKt.LaunchedEffect(
            ComposeBridges.BoxKey(key1),
            ComposeBridges.BoxKey(key2),
            ComposeBridges.BoxKey(key3),
            new LaunchedEffectBody(body),
            composer,
            _changed: 0);
    }

    /// <summary>
    /// Compose's <c>derivedStateOf { calculation() }</c>: returns a
    /// read-only <see cref="DerivedState{T}"/> whose value is lazily
    /// computed by <paramref name="calculation"/>. Compose tracks
    /// which state values <paramref name="calculation"/> reads and
    /// only re-runs it when one of them changes. Cache the returned
    /// instance via <see cref="Remember{T}(System.Func{T}, int, string)"/>
    /// so it survives recomposition:
    /// <code>
    /// var name  = Remember(() =&gt; new MutableState&lt;string&gt;("Ada"));
    /// var greet = Remember(() =&gt; Compose.DerivedStateOf(() =&gt; $"Hi, {name.Value}!"));
    /// new Text(greet.Value); // recomposes only when name.Value changes
    /// </code>
    /// </summary>
    public static DerivedState<T> DerivedStateOf<T>(System.Func<T> calculation)
    {
        if (calculation is null) throw new System.ArgumentNullException(nameof(calculation));
        var jcw = new ObjectFunction0(() => MutableState<T>.ToJava(calculation()));
        var state = SnapshotStateKt.DerivedStateOf(jcw);
        return new DerivedState<T>(state);
    }

    /// <summary>
    /// C# parity for Kotlin's
    /// <c>produceState(initialValue, vararg keys) { producer }</c>:
    /// remembers a <see cref="MutableState{T}"/> seeded with
    /// <paramref name="initialValue"/>. Starts <paramref name="producer"/>
    /// the first time this call site enters the composition. The
    /// producer receives the state to write to plus a
    /// <see cref="System.Threading.CancellationToken"/> that fires
    /// when this call site leaves the composition.
    ///
    /// <code>
    /// var clock = Compose.ProduceState(
    ///     initialValue: "—",
    ///     producer: async (state, ct) =&gt;
    ///     {
    ///         while (!ct.IsCancellationRequested)
    ///         {
    ///             state.Value = System.DateTime.Now.ToLongTimeString();
    ///             await System.Threading.Tasks.Task.Delay(1000, ct);
    ///         }
    ///     });
    /// new Text(clock.Value);
    /// </code>
    ///
    /// Producer exceptions are logged via
    /// <see cref="Android.Util.Log.Error(string?, string?)"/> under
    /// the <c>ComposeNet</c> tag rather than becoming unobserved
    /// task exceptions. Writes from the producer are forwarded to
    /// the composition thread via <see cref="MutableState{T}.Value"/>
    /// — Compose handles the recomposition scheduling.
    /// </summary>
    /// <remarks>
    /// Implemented purely in C# (not via Kotlin's
    /// <c>SnapshotStateKt.ProduceState</c>) to keep the producer a
    /// plain <see cref="System.Threading.Tasks.Task"/> rather than a
    /// Kotlin suspend lambda. The lifecycle is driven
    /// by an <see cref="IRememberObserver"/> JCW that's the direct
    /// slot value, so Compose's runtime fires
    /// <c>onRemembered</c>/<c>onForgotten</c>/<c>onAbandoned</c>
    /// at the right times.
    /// </remarks>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        System.Func<MutableState<T>, System.Threading.CancellationToken, System.Threading.Tasks.Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(initialValue, producer, keys: null, line, file);

    /// <summary>
    /// Keyed <c>produceState(initial, key1) { producer }</c>: cancels
    /// the running producer and starts a fresh one whenever
    /// <paramref name="key1"/> changes (structural equality).
    /// </summary>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        object? key1,
        System.Func<MutableState<T>, System.Threading.CancellationToken, System.Threading.Tasks.Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(initialValue, producer, new[] { key1 }, line, file);

    /// <summary>Keyed <c>produceState(initial, key1, key2) { producer }</c>.</summary>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        object? key1,
        object? key2,
        System.Func<MutableState<T>, System.Threading.CancellationToken, System.Threading.Tasks.Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(initialValue, producer, new[] { key1, key2 }, line, file);

    /// <summary>Keyed <c>produceState(initial, key1, key2, key3) { producer }</c>.</summary>
    public static MutableState<T> ProduceState<T>(
        T initialValue,
        object? key1,
        object? key2,
        object? key3,
        System.Func<MutableState<T>, System.Threading.CancellationToken, System.Threading.Tasks.Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(initialValue, producer, new[] { key1, key2, key3 }, line, file);

    /// <summary>
    /// Array-form keyed <c>produceState(initial, vararg keys) { producer }</c>.
    /// Use when there are more than three keys.
    /// </summary>
    public static MutableState<T> ProduceStateKeyed<T>(
        T initialValue,
        object?[] keys,
        System.Func<MutableState<T>, System.Threading.CancellationToken, System.Threading.Tasks.Task> producer,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
        => ProduceStateCore(initialValue, producer, keys ?? throw new System.ArgumentNullException(nameof(keys)), line, file);

    static MutableState<T> ProduceStateCore<T>(
        T initialValue,
        System.Func<MutableState<T>, System.Threading.CancellationToken, System.Threading.Tasks.Task> producer,
        object?[]? keys,
        int line,
        string file)
    {
        if (producer is null) throw new System.ArgumentNullException(nameof(producer));
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.ProduceState<T> must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            // The slot value MUST be the IRememberObserver itself —
            // Compose only inspects what UpdateRememberedValue is
            // handed for the IRememberObserver interface. Nesting it
            // inside RememberHolder would silently break the
            // OnRemembered/OnForgotten/OnAbandoned hooks.
            if (composer.RememberedValue() is ProduceStateScope<T> existing)
            {
                if (!RememberHolder.KeysEqual(existing.Keys, keys))
                    existing.Restart(keys);
                return existing.State;
            }
            var scope = new ProduceStateScope<T>(initialValue, producer, keys);
            composer.UpdateRememberedValue(scope);
            return scope.State;
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    /// <summary>
    /// Bridges Compose's <c>snapshotFlow { producer() }</c> into an
    /// <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/>.
    /// Every time any <see cref="MutableState{T}"/> /
    /// <see cref="MutableNumberState{T}"/> read inside
    /// <paramref name="producer"/> is written to and the surrounding
    /// snapshot is applied, the producer re-runs on Compose's main
    /// dispatcher and the new value flows through the returned
    /// async sequence. Duplicate consecutive values (compared via
    /// Java <c>equals</c>) are suppressed by Kotlin's
    /// <c>snapshotFlow</c> — the C# consumer only sees genuine
    /// changes.
    /// </summary>
    /// <typeparam name="T">
    /// Value type produced. Must round-trip through
    /// <see cref="MutableState{T}"/>'s boxing helpers — the
    /// supported set is .NET primitives (<see cref="byte"/>,
    /// <see cref="sbyte"/>, <see cref="short"/>, <see cref="ushort"/>,
    /// <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>,
    /// <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>,
    /// <see cref="bool"/>, <see cref="char"/>), <see cref="string"/>,
    /// <see cref="Java.Lang.Object"/> peers, and
    /// <see cref="System.Nullable{T}"/> of any of those primitives.
    /// Tuples / value tuples / arbitrary CLR structs are not
    /// supported.
    /// </typeparam>
    /// <param name="producer">
    /// Lambda that reads one or more Compose state values and
    /// returns a derived value. <strong>Not</strong>
    /// <c>@Composable</c> — do not call <c>Compose.Remember</c> or
    /// any other Compose-runtime helper from inside. Reads do not
    /// have to be wrapped in <see cref="DerivedStateOf{T}"/>;
    /// <c>snapshotFlow</c> records dependencies automatically via
    /// snapshot tracking.
    /// </param>
    /// <returns>
    /// An async sequence that yields each new producer value on the
    /// Compose main thread until the consumer disposes the
    /// enumerator or cancels the <see cref="System.Threading.CancellationToken"/>
    /// passed to <c>WithCancellation</c>. Disposing or cancelling
    /// tears down the underlying Kotlin coroutine and unregisters
    /// the snapshot-apply observer.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <strong>Back-pressure:</strong> values are buffered through a
    /// bounded(1) channel with <c>DropOldest</c> conflation. A slow
    /// consumer naturally sees only the latest value between reads —
    /// matching the behaviour of Kotlin's own <c>collect</c> when
    /// the collector can't keep up. <c>emit</c> never blocks
    /// Kotlin's dispatcher, so there's no risk of deadlock against a
    /// C# awaiter resuming on the same main thread.
    /// </para>
    /// <para>
    /// <strong>Canonical use case:</strong> reacting to
    /// <c>LazyListState</c> scroll position, e.g.
    /// <c>Compose.SnapshotFlow(() => listState.FirstVisibleItemIndex)</c>,
    /// to fire analytics or fetch more data when the user scrolls
    /// past a threshold. Pair with <c>Compose.LaunchedEffect</c> so
    /// the collection's lifetime is tied to the composition that
    /// started it.
    /// </para>
    /// <para>
    /// Each call to <see cref="System.Collections.Generic.IAsyncEnumerable{T}.GetAsyncEnumerator"/>
    /// starts a fresh Kotlin coroutine — the returned enumerable is
    /// not multicast.
    /// </para>
    /// </remarks>
    public static System.Collections.Generic.IAsyncEnumerable<T> SnapshotFlow<T>(System.Func<T> producer)
    {
        System.ArgumentNullException.ThrowIfNull(producer);
        return new SnapshotFlowEnumerable<T>(producer);
    }
}
