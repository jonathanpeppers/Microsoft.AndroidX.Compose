using AndroidX.Compose.Material3;
using AndroidX.Compose.UI.Platform;

namespace ComposeNet;

/// <summary>
/// Static host for the built-in Compose composition locals exposed
/// by the official bindings. Each member returns a
/// <see cref="CompositionLocal{T}"/> typed against the value Compose
/// supplies at the root of the composition (no
/// <c>CompositionLocalProvider</c> required to <em>read</em> them —
/// they always have a default in scope inside a
/// <c>ComposeActivity</c>).
///
/// <para>The Kotlin originals are exposed as top-level <c>val</c>s
/// (e.g. <c>LocalContext</c> in
/// <c>androidx.compose.ui.platform.AndroidCompositionLocals.android.kt</c>);
/// we surface them as properties on a <c>Locals</c> class so a single
/// <c>using static ComposeNet.Locals;</c> imports them all without
/// risking a clash with user types named <c>LocalContext</c>, etc.</para>
///
/// <para><b>Not yet bound.</b> <c>LocalDensity</c>,
/// <c>LocalContentColor</c>, and <c>LocalTextStyle</c> are stripped
/// from the current bindings (they depend on <c>@JvmInline value
/// class</c> parameters the binder can't surface). Add them via
/// <c>[ComposeBridge]</c> declarations once the upstream binder fix
/// (dotnet/java-interop#1440) lands or once we accept the bridge
/// boilerplate as a stopgap.</para>
/// </summary>
public static class Locals
{
    /// <summary>
    /// The Android <see cref="Android.Content.Context"/> attached to
    /// the host <c>ComposeView</c>. Equivalent to Kotlin's
    /// <c>LocalContext.current</c>.
    /// </summary>
    public static readonly CompositionLocal<Android.Content.Context> LocalContext =
        new(AndroidCompositionLocals_androidKt.LocalContext);

    /// <summary>
    /// The current <see cref="Android.Content.Res.Configuration"/>,
    /// updated when the system delivers a configuration change (locale,
    /// orientation, dark mode, etc.).
    /// </summary>
    public static readonly CompositionLocal<Android.Content.Res.Configuration> LocalConfiguration =
        new(AndroidCompositionLocals_androidKt.LocalConfiguration);

    /// <summary>
    /// The <see cref="AndroidX.Lifecycle.ILifecycleOwner"/> that hosts
    /// the current composition (typically the host activity or
    /// fragment). Use this to scope coroutines / effects to the same
    /// lifecycle as the UI tree.
    /// </summary>
    /// <remarks>
    /// We bind the legacy
    /// <c>androidx.compose.ui.platform.LocalLifecycleOwner</c> here
    /// instead of the newer
    /// <c>androidx.lifecycle.compose.LocalLifecycleOwner</c> because
    /// the lifecycle-runtime-compose binding currently strips that
    /// type. The legacy accessor is marked <c>@Deprecated</c> in
    /// Kotlin but still delegates to the same Composition local at
    /// runtime — swap once the upstream binding exposes it.
    /// </remarks>
#pragma warning disable CS0618 // legacy LocalLifecycleOwner, see <remarks>
    public static readonly CompositionLocal<AndroidX.Lifecycle.ILifecycleOwner> LocalLifecycleOwner =
        new(AndroidCompositionLocals_androidKt.LocalLifecycleOwner);
#pragma warning restore CS0618

    /// <summary>
    /// The current <see cref="Android.Content.Res.Resources"/>,
    /// already configuration-aware (re-read on configuration change).
    /// </summary>
    public static readonly CompositionLocal<Android.Content.Res.Resources> LocalResources =
        new(AndroidCompositionLocals_androidKt.LocalResources);

    /// <summary>
    /// The hosting <see cref="Android.Views.View"/> — usually the
    /// <c>AbstractComposeView</c> embedding this composition.
    /// </summary>
    public static readonly CompositionLocal<Android.Views.View> LocalView =
        new(AndroidCompositionLocals_androidKt.LocalView);

    /// <summary>
    /// The Material 3 <see cref="ColorScheme"/>
    /// installed by the nearest
    /// <see cref="MaterialTheme"/>. Reading this directly is rarely
    /// necessary — most consumers go through <c>MaterialTheme</c>'s
    /// own accessors — but it's exposed for parity with the Kotlin
    /// API and for callers that want to override the scheme inside a
    /// subtree without redefining the whole theme.
    /// </summary>
    public static readonly CompositionLocal<ColorScheme> LocalColorScheme =
        new(ColorSchemeKt.LocalColorScheme);
}
