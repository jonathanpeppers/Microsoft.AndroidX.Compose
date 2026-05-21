using Android.OS;
using Android.Runtime;
using AndroidX.Activity;
using Androidx.Compose.Foundation.Layout;
using Androidx.Compose.Material3;
using Androidx.Compose.Runtime;
using Androidx.Compose.Runtime.Internal;
using Androidx.Compose.UI;
using Androidx.Compose.UI.Platform;
using Kotlin.Jvm.Functions;

namespace ComposeNet.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@android:style/Theme.Material.Light")]
public class MainActivity : ComponentActivity
{
    const string TAG = "ComposeNet";

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var count = SnapshotStateKt.MutableStateOf(
            Java.Lang.Integer.ValueOf(0),
            SnapshotStateKt.StructuralEqualityPolicy());

        var composeView = new ComposeView(this);
        composeView.SetContent(ComposableLambdaKt.ComposableLambdaInstance(
            key: -1, tracked: false, block: new ThemedRoot(count)));

        // On API 36 / Theme.Material.Light, the ActionBar is drawn ON TOP of
        // android.R.id.content (overlay decor), so we need to push the
        // ComposeView down by the action bar height. The bottom system nav
        // bar still overlaps in edge-to-edge mode. Side padding is cosmetic.
        composeView.SetPadding(
            left:   Dp(16),
            top:    SystemBarHeight("status_bar_height") + ActionBarHeight() + Dp(16),
            right:  Dp(16),
            bottom: SystemBarHeight("navigation_bar_height") + Dp(16));

        SetContentView(composeView);

        // After SetContentView the DecorView is realized, so Window is
        // guaranteed non-null below.
        var window = Window;
        System.Diagnostics.Debug.Assert(window != null, "Window should be non-null after SetContentView");

        // We want dark status-bar icons so the clock / battery / etc. are
        // readable against the light Material theme background.
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            // API 30+: WindowInsetsController is the modern API.
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
            // API 23–29: the only way to request dark status-bar icons is
            // the SystemUiVisibility flag on the DecorView. The API was
            // deprecated in API 30 in favor of WindowInsetsController, but
            // remains the correct call below it.
            var decor = window.DecorView;
#pragma warning disable CA1422 // Validate platform compatibility
            decor.SystemUiFlags |= Android.Views.SystemUiFlags.LightStatusBar;
#pragma warning restore CA1422
        }
        Android.Util.Log.Debug(TAG, "OnCreate complete");
    }

    int ActionBarHeight()
    {
        var tv = new Android.Util.TypedValue();
        if (Theme!.ResolveAttribute(Android.Resource.Attribute.ActionBarSize, tv, true))
            return Android.Util.TypedValue.ComplexToDimensionPixelSize(tv.Data, Resources!.DisplayMetrics);
        return 0;
    }

    int Dp(int dp) => (int)(dp * Resources!.DisplayMetrics!.Density);

    int SystemBarHeight(string resName)
    {
        int id = Resources!.GetIdentifier(resName, "dimen", "android");
        return id > 0 ? Resources.GetDimensionPixelSize(id) : 0;
    }
}

// Top-level content: wrap the actual UI in MaterialTheme so child composables
// (Button, etc.) read Material color/typography/shape defaults from the
// CompositionLocal stack.
[Register("composenet/sample/ThemedRoot")]
public sealed class ThemedRoot : Java.Lang.Object, IFunction2
{
    readonly AppContent _body;
    public ThemedRoot(IMutableState count) => _body = new AppContent(count);

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0!);

        // Use Android 12+ dynamic colors derived from the system wallpaper
        // (Material You). On a stock emulator this gives the Google
        // baseline blue/teal palette instead of the Compose-default purple.
        var scheme = DynamicTonalPaletteKt.DynamicLightColorScheme(Android.App.Application.Context);

        // MaterialTheme(colorScheme, shapes, typography, content, composer, $changed, $default)
        // $default = 0b0110 → defaults for shapes and typography; provide
        // colorScheme (bit 0) and content (bit 3).
        MaterialThemeKt.MaterialTheme(
            colorScheme: scheme,
            shapes:      null,
            typography:  null,
            content:     _body,
            _composer:   composer,
            p5:          0,
            _changed:    0b0110);
        return null;
    }
}

[Register("composenet/sample/AppContent")]
public sealed class AppContent : Java.Lang.Object, IFunction2
{
    readonly IMutableState _count;
    readonly ColumnContent _content;

    public AppContent(IMutableState count)
    {
        _count = count;
        _content = new ColumnContent(_count);
    }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0!);

        // Column(Modifier, Arrangement.Vertical, Alignment.Horizontal, content, composer, $changed, $default)
        ColumnKt.Column(
            modifier: null,
            verticalArrangement: null,
            horizontalAlignment: null,
            content: _content,
            _composer: composer,
            p5: 0,
            _changed: 0b0111);
        return null;
    }
}

[Register("composenet/sample/ColumnContent")]
public sealed class ColumnContent : Java.Lang.Object, IFunction3
{
    readonly IMutableState _count;
    readonly ButtonLabel  _buttonLabel;
    readonly ClickHandler _click;

    public ColumnContent(IMutableState count)
    {
        _count       = count;
        _buttonLabel = new ButtonLabel();
        _click       = new ClickHandler(count);
    }

    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p1!);

        int n = ((Java.Lang.Integer)_count.Value!).IntValue();

        // Use Material `Text` for the body — it picks up MaterialTheme's
        // typography and reads LocalContentColor from the composition
        // (so themed/colored properly).
        ComposeApi.Text("Hello from .NET", composer);
        ComposeApi.Text("Count: " + n, composer);

        // Material 3 filled Button — colors, shape, ripple, elevation all come
        // from MaterialTheme defaults higher up the composition.
        ComposeApi.Button(_click, _buttonLabel, composer);

        return null;
    }
}

// Content lambda of the Material Button. The receiver is RowScope, which we
// don't use — just emit a label.
[Register("composenet/sample/ButtonLabel")]
public sealed class ButtonLabel : Java.Lang.Object, IFunction3
{
    public Java.Lang.Object? Invoke(Java.Lang.Object? rowScope, Java.Lang.Object? p1, Java.Lang.Object? p2)
    {
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p1!);
        // Material `Text` here picks up LocalContentColor from the Button,
        // which is `onPrimary` (white on the default blue primary).
        ComposeApi.Text("Tap to increment", composer);
        return null;
    }
}

[Register("composenet/sample/ClickHandler")]
public sealed class ClickHandler : Java.Lang.Object, IFunction0
{
    readonly IMutableState _count;
    public ClickHandler(IMutableState count) => _count = count;

    public Java.Lang.Object? Invoke()
    {
        int current = ((Java.Lang.Integer)_count.Value!).IntValue();
        _count.Value = Java.Lang.Integer.ValueOf(current + 1);
        return null;
    }
}
