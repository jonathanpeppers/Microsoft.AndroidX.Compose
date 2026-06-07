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
    {
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "Compose.Remember<T> must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            if (composer.RememberedValue() is RememberHolder existing)
                return (T)existing.Value!;
            var value = factory();
            composer.UpdateRememberedValue(new RememberHolder(value));
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
    /// on restore. The <c>vararg inputs</c> dependency-tracking
    /// parameter isn't surfaced here — an empty <c>Object[]</c> is
    /// passed, meaning the cached value is never invalidated on
    /// dependency change. A future <c>params object?[]</c> overload
    /// can lift that.
    /// </summary>
    public static T RememberSaveable<T>(
        System.Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
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
                return RememberSaveableWrapper(factory, composer);

            return RememberSaveableScalar(factory, composer);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    static T RememberSaveableScalar<T>(System.Func<T> factory, IComposer composer)
    {
        var jcw = new ObjectFunction0(() => MutableState<T>.ToJava(factory()));
        var handle = ComposeBridges.RememberSaveableSimple(
            ComposeBridges.EmptyObjectArray(),
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
            System.GC.KeepAlive(composer);
        }
    }

    static T RememberSaveableWrapper<T>(System.Func<T> factory, IComposer composer)
    {
        // Cache the C# wrapper across recompositions so we don't
        // allocate a fresh facade + Kotlin IMutableState on every
        // render. `Compose.Remember` opens its own nested replaceable
        // group; nesting is fine — Compose's slot table handles it.
        var wrapper = Remember(factory)
            ?? throw new System.InvalidOperationException(
                $"Compose.RememberSaveable<{typeof(T).Name}>: factory returned null.");
        var iwrap = (IMutableStateWrapper)wrapper;

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
            ComposeBridges.EmptyObjectArray(),
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
}

