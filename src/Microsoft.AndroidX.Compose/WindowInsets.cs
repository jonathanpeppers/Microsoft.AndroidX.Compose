using Android.Runtime;
using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;
using BindingWindowInsetsAndroid = AndroidX.Compose.Foundation.Layout.WindowInsets_androidKt;
using BindingWindowInsetsKt = AndroidX.Compose.Foundation.Layout.WindowInsetsKt;

namespace AndroidX.Compose;

/// <summary>
/// C# wrapper around Compose Foundation's <c>WindowInsets</c>.
/// Use the composition-aware static methods such as
/// <see cref="SafeDrawing(IComposer)"/> to read live platform insets,
/// or construct fixed insets explicitly.
/// </summary>
public sealed class WindowInsets : Java.Lang.Object
{
    readonly IWindowInsets _jvm;

    WindowInsets(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer)
    {
        _jvm = this.JavaCast<IWindowInsets>();
    }

    /// <summary>
    /// Creates fixed insets in density-independent pixels.
    /// </summary>
    public WindowInsets(
        Dp left = default,
        Dp top = default,
        Dp right = default,
        Dp bottom = default)
        : this(
            BuildHandle(BindingWindowInsetsKt.WindowInsets(
                left.Value,
                top.Value,
                right.Value,
                bottom.Value)),
            JniHandleOwnership.TransferLocalRef)
    {
    }

    internal IWindowInsets Jvm => _jvm;

    /// <summary>Reads the current caption-bar insets.</summary>
    public static WindowInsets CaptionBar(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetCaptionBar);

    /// <summary>Reads the current display-cutout insets.</summary>
    public static WindowInsets DisplayCutout(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetDisplayCutout);

    /// <summary>Reads the current input-method-editor insets.</summary>
    public static WindowInsets Ime(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetIme);

    /// <summary>Reads the current mandatory-system-gesture insets.</summary>
    public static WindowInsets MandatorySystemGestures(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetMandatorySystemGestures);

    /// <summary>Reads the current navigation-bar insets.</summary>
    public static WindowInsets NavigationBars(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetNavigationBars);

    /// <summary>
    /// Reads insets safe for both drawing and interactive content.
    /// </summary>
    public static WindowInsets SafeContent(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetSafeContent);

    /// <summary>
    /// Reads insets that keep drawing clear of system UI and cutouts.
    /// </summary>
    public static WindowInsets SafeDrawing(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetSafeDrawing);

    /// <summary>
    /// Reads insets that keep interactive content clear of system gestures.
    /// </summary>
    public static WindowInsets SafeGestures(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetSafeGestures);

    /// <summary>Reads the current status-bar insets.</summary>
    public static WindowInsets StatusBars(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetStatusBars);

    /// <summary>Reads the union of status-bar and navigation-bar insets.</summary>
    public static WindowInsets SystemBars(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetSystemBars);

    /// <summary>Reads the current system-gesture insets.</summary>
    public static WindowInsets SystemGestures(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetSystemGestures);

    /// <summary>Reads insets where taps are intercepted by system UI.</summary>
    public static WindowInsets TappableElement(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetTappableElement);

    /// <summary>Reads waterfall-display edge insets.</summary>
    public static WindowInsets Waterfall(IComposer composer) =>
        Read(composer, BindingWindowInsetsAndroid.GetWaterfall);

    /// <summary>Adds each edge of <paramref name="insets"/> to this value.</summary>
    public WindowInsets Add(WindowInsets insets)
    {
        ArgumentNullException.ThrowIfNull(insets);
        return Wrap(BindingWindowInsetsKt.Add(_jvm, insets._jvm));
    }

    /// <summary>
    /// Takes the maximum edge from this value and <paramref name="insets"/>.
    /// </summary>
    public WindowInsets Union(WindowInsets insets)
    {
        ArgumentNullException.ThrowIfNull(insets);
        return Wrap(BindingWindowInsetsKt.Union(_jvm, insets._jvm));
    }

    /// <summary>
    /// Subtracts <paramref name="insets"/> from each edge, clamped at zero.
    /// </summary>
    public WindowInsets Exclude(WindowInsets insets)
    {
        ArgumentNullException.ThrowIfNull(insets);
        return Wrap(BindingWindowInsetsKt.Exclude(_jvm, insets._jvm));
    }

    /// <summary>Returns only the selected <paramref name="sides"/>.</summary>
    public WindowInsets Only(WindowInsetsSides sides) =>
        Wrap(BindingWindowInsetsKt.Only(_jvm, (int)sides));

    /// <summary>
    /// Converts these insets to composition-aware <see cref="PaddingValues"/>.
    /// </summary>
    public PaddingValues AsPaddingValues(IComposer composer)
    {
        ArgumentNullException.ThrowIfNull(composer);
        return PaddingValues.Wrap(
            BindingWindowInsetsKt.AsPaddingValues(_jvm, composer, 0));
    }

    static WindowInsets Read(
        IComposer composer,
        Func<WindowInsetsCompanion, IComposer?, int, IWindowInsets> getter)
    {
        ArgumentNullException.ThrowIfNull(composer);
        return Wrap(getter(IWindowInsets.Companion, composer, 0));
    }

    static WindowInsets Wrap(IWindowInsets insets) =>
        new(BuildHandle(insets), JniHandleOwnership.TransferLocalRef);

    // Why raw JNI: the bound factory/getters return an interface peer;
    // this wrapper needs an independent local ref before the temporary
    // managed interface peer can be collected.
    static IntPtr BuildHandle(IWindowInsets insets) =>
        JNIEnv.NewLocalRef(((Java.Lang.Object)insets).Handle);
}
