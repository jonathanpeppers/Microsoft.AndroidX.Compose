using System.Runtime.CompilerServices;
using Android.Util;
using AndroidX.Activity;
using AndroidX.Compose.Runtime.Internal;
using AndroidX.Compose.UI.Platform;

namespace ComposeNet;

/// <summary>
/// Base activity for ComposeNet apps. Mirrors the Kotlin shape:
/// override <c>OnCreate</c>, call <c>base.OnCreate</c>, then call
/// <see cref="SetContent(Func{ComposableNode})"/> with a lambda
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
///     <c>Modifier.Companion.SystemBarsPadding()</c>) so it stays inside the safe area.
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

    /// <summary>
    /// Returns the value of <paramref name="factory"/> the first time
    /// this call-site is reached, then the cached value on subsequent
    /// recompositions. Use to create <see cref="MutableState{T}"/> /
    /// <see cref="MutableNumberState{T}"/> instances at the top of a
    /// <see cref="SetContent"/> lambda.
    ///
    /// Forwards to <see cref="Compose.Remember{T}(Func{T}, int, string)"/>,
    /// which is backed by the active composer's slot table — so the cached
    /// value survives recomposition the same way Kotlin's <c>remember { }</c>
    /// does, and clears with the composition (not the activity).
    /// </summary>
    protected T Remember<T>(
        Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.Remember(factory, line, file);

    /// <summary>Keyed <c>remember(key1) { factory() }</c>; forwards to <see cref="Compose.Remember{T}(Func{T}, object?, int, string)"/>.</summary>
    protected T Remember<T>(
        Func<T> factory,
        object? key1,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.Remember(factory, key1, line, file);

    /// <summary>Keyed <c>remember(key1, key2) { factory() }</c>.</summary>
    protected T Remember<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.Remember(factory, key1, key2, line, file);

    /// <summary>Keyed <c>remember(key1, key2, key3) { factory() }</c>.</summary>
    protected T Remember<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        object? key3,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.Remember(factory, key1, key2, key3, line, file);

    /// <summary>Array-form keyed <c>remember(vararg keys) { factory() }</c>.</summary>
    protected T RememberKeyed<T>(
        Func<T> factory,
        object?[] keys,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.RememberKeyed(factory, keys, line, file);

    /// <summary>
    /// Like <see cref="Remember{T}(Func{T}, int, string)"/>, but
    /// the cached value also survives process death / activity
    /// recreation through Compose's <c>SaveableStateRegistry</c>.
    /// Mirrors Kotlin's single <c>rememberSaveable&lt;T&gt;</c> entry
    /// point — works for scalar values and for state-holder wrappers
    /// (<see cref="MutableState{U}"/> / <see cref="MutableNumberState{U}"/>).
    /// See <see cref="Compose.RememberSaveable{T}(Func{T}, int, string)"/>
    /// for the full supported-<c>T</c> list.
    /// </summary>
    protected T RememberSaveable<T>(
        Func<T> factory,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.RememberSaveable(factory, line, file);

    /// <summary>Keyed <c>rememberSaveable(key1) { factory() }</c>; forwards to <see cref="Compose.RememberSaveable{T}(Func{T}, object?, int, string)"/>.</summary>
    protected T RememberSaveable<T>(
        Func<T> factory,
        object? key1,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.RememberSaveable(factory, key1, line, file);

    /// <summary>Keyed <c>rememberSaveable(key1, key2) { factory() }</c>.</summary>
    protected T RememberSaveable<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.RememberSaveable(factory, key1, key2, line, file);

    /// <summary>Keyed <c>rememberSaveable(key1, key2, key3) { factory() }</c>.</summary>
    protected T RememberSaveable<T>(
        Func<T> factory,
        object? key1,
        object? key2,
        object? key3,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.RememberSaveable(factory, key1, key2, key3, line, file);

    /// <summary>Array-form keyed <c>rememberSaveable(vararg inputs) { factory() }</c>.</summary>
    protected T RememberSaveableKeyed<T>(
        Func<T> factory,
        object?[] keys,
        [CallerLineNumber] int line = 0,
        [CallerFilePath]   string file = "")
        => Compose.RememberSaveableKeyed(factory, keys, line, file);

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
    protected void SetContent(Func<ComposableNode> content)
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
