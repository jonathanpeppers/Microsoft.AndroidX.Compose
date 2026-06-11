using Android.Content;
using Android.Runtime;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.ViewInterop;
using Kotlin.Jvm.Functions;
using AView = Android.Views.View;

namespace AndroidX.Compose;

/// <summary>
/// C# facade over Compose's <c>androidx.compose.ui.viewinterop.AndroidView</c>
/// interop composable — hosts a stock Android <see cref="AView"/>
/// inside the enclosing Compose composition. Use when you need to
/// embed something Compose doesn't render natively (a third-party
/// custom view, a not-yet-converted control, a MAUI handler's
/// <c>ToPlatform()</c> result, …).
/// </summary>
/// <remarks>
/// <para>The <see cref="Factory"/> lambda runs <em>once</em> per
/// composition slot — Compose caches the materialised view and
/// reuses it across recompositions. The <see cref="Update"/> lambda,
/// if supplied, runs on each recomposition so callers can push fresh
/// property values into the cached view.</para>
///
/// <para>The Kotlin signature takes a <c>(Context) -&gt; T</c>
/// factory and an optional <c>(T) -&gt; Unit</c> update. Both
/// lambdas execute on Compose's main thread.</para>
/// </remarks>
public sealed class AndroidView : ComposableNode
{
    readonly Func<Context, AView> _factory;
    readonly Action<AView>? _update;

    /// <summary>
    /// Create a new <see cref="AndroidView"/> wrapping the result of
    /// <paramref name="factory"/>.
    /// </summary>
    /// <param name="factory">Materialiser called once per composition
    /// slot. Compose hands it the runtime
    /// <see cref="Context"/>.</param>
    /// <param name="update">Optional per-recomposition update hook.
    /// Receives the cached view; mutate its properties to push state
    /// changes Compose can't observe natively.</param>
    public AndroidView(Func<Context, AView> factory, Action<AView>? update = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
        _update  = update;
    }

    /// <inheritdoc cref="ComposableNode.Render(IComposer)"/>
    public override void Render(IComposer composer)
    {
        var modifier   = BuildModifier();
        var updateJcw  = _update is null ? null : new AndroidViewUpdateAdapter(_update);
        var factoryJcw = new AndroidViewFactoryAdapter(_factory);

        // Bit positions in AndroidView's $default mask:
        //   0 = factory   (always supplied)
        //   1 = modifier
        //   2 = update
        int defaults = 0;
        if (modifier is null)  defaults |= 1 << 1;
        if (updateJcw is null) defaults |= 1 << 2;

        AndroidView_androidKt.AndroidView(
            factory:   factoryJcw,
            modifier:  modifier,
            update:    updateJcw,
            _composer: composer,
            // Binder names are positionally derived and misleading —
            // double-checked via ilspycmd against
            // `AndroidView(...,Composer,II)V`:
            //   `p4`        = Kotlin's `$changed`
            //   `_changed`  = Kotlin's `$default`
            // (Kotlin lowering always emits `$changed` before
            // `$default`.)
            p4:        0,
            _changed:  defaults);
    }
}

/// <summary>
/// <c>Function1&lt;Context, View&gt;</c> JCW used by
/// <see cref="AndroidView"/> as Compose's <c>factory</c> argument.
/// </summary>
[Register("net/compose/AndroidViewFactoryAdapter")]
internal sealed class AndroidViewFactoryAdapter : Java.Lang.Object, IFunction1
{
    readonly Func<Context, AView> _factory;

    public AndroidViewFactoryAdapter(Func<Context, AView> factory) => _factory = factory;

    public Java.Lang.Object Invoke(Java.Lang.Object? p0) =>
        _factory((Context)p0!);
}

/// <summary>
/// <c>Function1&lt;View, Unit&gt;</c> JCW used by
/// <see cref="AndroidView"/> as Compose's optional <c>update</c>
/// argument.
/// </summary>
[Register("net/compose/AndroidViewUpdateAdapter")]
internal sealed class AndroidViewUpdateAdapter : Java.Lang.Object, IFunction1
{
    static Java.Lang.Object? s_unit;

    readonly Action<AView> _update;

    public AndroidViewUpdateAdapter(Action<AView> update) => _update = update;

    public Java.Lang.Object Invoke(Java.Lang.Object? p0)
    {
        _update((AView)p0!);
        // Kotlin `Unit`. Returning null would fault inside Compose's
        // adapter; a singleton wrapper keeps allocations off the hot
        // recomposition path.
        return s_unit ??= Kotlin.Unit.Instance!;
    }
}
