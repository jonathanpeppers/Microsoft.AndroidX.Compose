using Android.Runtime;
using AndroidX.Compose.Foundation.Layout;
using AndroidX.Compose.Runtime;
using BindingWindowInsetsKt = AndroidX.Compose.Foundation.Layout.WindowInsetsKt;

namespace AndroidX.Compose;

/// <summary>
/// C# wrapper around Compose Foundation's <c>WindowInsets</c>.
/// Use composition-aware readers such as
/// <see cref="Composables.SafeDrawingInsets"/> to read live platform
/// insets, or construct fixed insets explicitly.
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

    /// <summary>
    /// Converts these insets to composition-aware <see cref="PaddingValues"/>
    /// using the active implicit composition.
    /// </summary>
    public PaddingValues AsPaddingValues() =>
        AsPaddingValues(ComposableContext.Current);

    internal static WindowInsets Wrap(IWindowInsets insets) =>
        new(BuildHandle(insets), JniHandleOwnership.TransferLocalRef);

    // Why raw JNI: the bound factory/getters return an interface peer;
    // this wrapper needs an independent local ref before the temporary
    // managed interface peer can be collected.
    static IntPtr BuildHandle(IWindowInsets insets) =>
        JNIEnv.NewLocalRef(((Java.Lang.Object)insets).Handle);
}
