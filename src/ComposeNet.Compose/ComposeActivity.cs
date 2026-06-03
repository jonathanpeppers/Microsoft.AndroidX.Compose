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
    /// Sets the activity's content view to a Compose composition that
    /// runs <paramref name="content"/> on each composition pass and
    /// renders the returned tree.
    /// </summary>
    protected void SetContent(System.Func<ComposableNode> content)
    {
        var view = new ComposeView(this);

        ApplySafeAreaPadding(view);

        view.SetContent(ComposableLambdaKt.ComposableLambdaInstance(
            key:     -1,
            tracked: false,
            block:   new ComposableLambda2(composer => content().Render(composer))));

        SetContentView(view);
        ApplyLightStatusBar();

        Log.Debug(TAG, "ComposeActivity content set");
    }

    void ApplySafeAreaPadding(Android.Views.View view)
    {
        // On API 36 / Theme.Material.Light the ActionBar is overlay-decor
        // and draws on top of android.R.id.content. Push the ComposeView
        // down by status + action bar height; pad the nav bar at bottom.
        view.SetPadding(
            left:   Dp(16),
            top:    SystemBarHeight("status_bar_height") + ActionBarHeight() + Dp(16),
            right:  Dp(16),
            bottom: SystemBarHeight("navigation_bar_height") + Dp(16));
    }

    void ApplyLightStatusBar()
    {
        var window = Window;
        System.Diagnostics.Debug.Assert(window != null, "Window non-null after SetContentView");

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            var insetsController = window.InsetsController;
            if (insetsController != null)
            {
                insetsController.SetSystemBarsAppearance(
                    (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars,
                    (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars);
            }
        }
        else
        {
            var decor = window.DecorView;
#pragma warning disable CA1422 // Validate platform compatibility
            decor.SystemUiFlags |= Android.Views.SystemUiFlags.LightStatusBar;
#pragma warning restore CA1422
        }
    }

    int ActionBarHeight()
    {
        var theme = Theme;
        var resources = Resources;
        System.Diagnostics.Debug.Assert(theme != null && resources != null);
        var tv = new TypedValue();
        if (theme.ResolveAttribute(Android.Resource.Attribute.ActionBarSize, tv, true))
            return TypedValue.ComplexToDimensionPixelSize(tv.Data, resources.DisplayMetrics);
        return 0;
    }

    int Dp(int dp)
    {
        var resources = Resources;
        System.Diagnostics.Debug.Assert(resources != null);
        return (int)(dp * resources.DisplayMetrics!.Density);
    }

    int SystemBarHeight(string resName)
    {
        var resources = Resources;
        System.Diagnostics.Debug.Assert(resources != null);
        int id = resources.GetIdentifier(resName, "dimen", "android");
        return id > 0 ? resources.GetDimensionPixelSize(id) : 0;
    }
}
