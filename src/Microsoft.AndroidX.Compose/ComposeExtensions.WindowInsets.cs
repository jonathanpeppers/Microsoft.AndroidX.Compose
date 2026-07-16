using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;
using BindingWindowInsetsAndroid = AndroidX.Compose.Foundation.Layout.WindowInsets_androidKt;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>Reads the current caption-bar insets.</summary>
    public static WindowInsets CaptionBarInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetCaptionBar);

    /// <summary>Reads the current display-cutout insets.</summary>
    public static WindowInsets DisplayCutoutInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetDisplayCutout);

    /// <summary>Reads the current input-method-editor insets.</summary>
    public static WindowInsets ImeInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetIme);

    /// <summary>Reads the current mandatory-system-gesture insets.</summary>
    public static WindowInsets MandatorySystemGesturesInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetMandatorySystemGestures);

    /// <summary>Reads the current navigation-bar insets.</summary>
    public static WindowInsets NavigationBarsInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetNavigationBars);

    /// <summary>Reads insets safe for both drawing and interactive content.</summary>
    public static WindowInsets SafeContentInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetSafeContent);

    /// <summary>Reads insets that keep drawing clear of system UI and cutouts.</summary>
    public static WindowInsets SafeDrawingInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetSafeDrawing);

    /// <summary>Reads insets that keep interactive content clear of system gestures.</summary>
    public static WindowInsets SafeGesturesInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetSafeGestures);

    /// <summary>Reads the current status-bar insets.</summary>
    public static WindowInsets StatusBarsInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetStatusBars);

    /// <summary>Reads the union of status-bar and navigation-bar insets.</summary>
    public static WindowInsets SystemBarsInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetSystemBars);

    /// <summary>Reads the current system-gesture insets.</summary>
    public static WindowInsets SystemGesturesInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetSystemGestures);

    /// <summary>Reads insets where taps are intercepted by system UI.</summary>
    public static WindowInsets TappableElementInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetTappableElement);

    /// <summary>Reads waterfall-display edge insets.</summary>
    public static WindowInsets WaterfallInsets(this IComposer composer) =>
        ReadWindowInsets(composer, BindingWindowInsetsAndroid.GetWaterfall);

    static WindowInsets ReadWindowInsets(
        IComposer composer,
        Func<WindowInsetsCompanion, IComposer?, int, IWindowInsets> getter)
    {
        ArgumentNullException.ThrowIfNull(composer);
        return WindowInsets.Wrap(getter(IWindowInsets.Companion, composer, 0));
    }
}
