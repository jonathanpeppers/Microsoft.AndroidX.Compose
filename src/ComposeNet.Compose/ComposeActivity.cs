using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Android.OS;
using Android.Util;
using AndroidX.Activity;
using AndroidX.Compose.Runtime.Internal;
using AndroidX.Compose.UI.Platform;

namespace ComposeNet;

/// <summary>
/// Base activity for ComposeNet apps. Mirrors the Kotlin shape:
/// override <c>OnCreate</c>, call <c>base.OnCreate</c>, then call
/// <see cref="SetContent(System.Func{ComposableNode})"/> with a lambda
/// returning the composition tree:
///
/// <code>
/// protected override void OnCreate(Bundle? state)
/// {
///     base.OnCreate(state);
///     SetContent(() => {
///         var count = Remember(() => new MutableNumberState&lt;int&gt;(0));
///         return new MaterialTheme {
///             new Column {
///                 new Text($"Count: {count}"),
///                 new Button(onClick: () => count++) { new Text("Tap") }
///             }
///         };
///     });
/// }
/// </code>
///
/// The activity opts the window into edge-to-edge in
/// <see cref="OnCreate(Bundle?)"/> via <see cref="EdgeToEdge.Enable(ComponentActivity)"/>
/// — system bars become transparent overlays, and Compose's
/// <c>WindowInsets</c> propagation handles inset padding. Pair this with a
/// <c>NoActionBar</c> framework / Material theme on the activity (e.g.
/// <c>@android:style/Theme.Material.Light.NoActionBar</c>) so no
/// framework <c>ActionBar</c> overlays the content.
///
/// Inset handling at the composition level:
/// <list type="bullet">
///   <item><description>
///     Inside a <see cref="Scaffold"/>, the top app bar / bottom bar slots
///     pad themselves; pad the body root with
///     <c>Modifier.Companion.SafeDrawingPadding()</c> when no top bar is
///     supplied so content doesn't draw under the status bar.
///   </description></item>
///   <item><description>
///     Outside a <see cref="Scaffold"/>, wrap the root composable in
///     <c>Modifier.Companion.SafeDrawingPadding()</c> (or
///     <c>SystemBarsPadding()</c>) so it stays inside the safe area.
///   </description></item>
/// </list>
///
/// The <c>content</c> lambda runs on every recomposition; tree allocation
/// is the per-recomposition cost of the Tier 1.5 facade (Tier 2 codegen
/// would skip it).
/// </summary>
public abstract class ComposeActivity : ComponentActivity
{
    const string TAG = "ComposeNet";

    // Activity-scoped Remember cache — tier-1.5 shim keyed off
    // [CallerLineNumber]. Survives recompositions but resets on
    // activity recreation (real fix would be rememberSaveable +
    // Composer.cache slot-table integration).
    readonly Dictionary<int, object?> _remembered = new();

    /// <summary>
    /// Returns the value of <paramref name="factory"/> the first time
    /// this call-site is reached, then the cached value on subsequent
    /// recompositions. Use to create <see cref="MutableState{T}"/> /
    /// <see cref="MutableNumberState{T}"/> instances at the top of a
    /// <see cref="SetContent"/> lambda.
    /// </summary>
    protected T Remember<T>(System.Func<T> factory, [CallerLineNumber] int key = 0)
    {
        if (!_remembered.TryGetValue(key, out var v))
            _remembered[key] = v = factory()!;
        return (T)v!;
    }

    /// <summary>
    /// Opts the window into edge-to-edge. Call <c>base.OnCreate</c>
    /// first thing in your subclass override — that runs
    /// <see cref="EdgeToEdge.Enable(ComponentActivity)"/> before
    /// <c>ComponentActivity.OnCreate</c>, which is the order the AndroidX
    /// API expects.
    /// </summary>
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        EdgeToEdge.Enable(this);
        base.OnCreate(savedInstanceState);
    }

    /// <summary>
    /// Sets the activity's content view to a Compose composition that
    /// runs <paramref name="content"/> on each composition pass and
    /// renders the returned tree.
    /// </summary>
    protected void SetContent(System.Func<ComposableNode> content)
    {
        var view = new ComposeView(this);

        view.SetContent(ComposableLambdaKt.ComposableLambdaInstance(
            key:     -1,
            tracked: false,
            block:   new ComposableLambda2(composer => content().Render(composer))));

        SetContentView(view);

        Log.Debug(TAG, "ComposeActivity content set");
    }
}
