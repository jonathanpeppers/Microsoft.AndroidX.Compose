namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Reads the current caption-bar insets.</summary>
    public static WindowInsets CaptionBarInsets() =>
        ComposeExtensions.CaptionBarInsets(ComposableContext.Current);

    /// <summary>Reads the current display-cutout insets.</summary>
    public static WindowInsets DisplayCutoutInsets() =>
        ComposeExtensions.DisplayCutoutInsets(ComposableContext.Current);

    /// <summary>Reads the current input-method-editor insets.</summary>
    public static WindowInsets ImeInsets() =>
        ComposeExtensions.ImeInsets(ComposableContext.Current);

    /// <summary>Reads the current mandatory-system-gesture insets.</summary>
    public static WindowInsets MandatorySystemGesturesInsets() =>
        ComposeExtensions.MandatorySystemGesturesInsets(ComposableContext.Current);

    /// <summary>Reads the current navigation-bar insets.</summary>
    public static WindowInsets NavigationBarsInsets() =>
        ComposeExtensions.NavigationBarsInsets(ComposableContext.Current);

    /// <summary>Reads insets safe for both drawing and interactive content.</summary>
    public static WindowInsets SafeContentInsets() =>
        ComposeExtensions.SafeContentInsets(ComposableContext.Current);

    /// <summary>Reads insets that keep drawing clear of system UI and cutouts.</summary>
    public static WindowInsets SafeDrawingInsets() =>
        ComposeExtensions.SafeDrawingInsets(ComposableContext.Current);

    /// <summary>Reads insets that keep interactive content clear of system gestures.</summary>
    public static WindowInsets SafeGesturesInsets() =>
        ComposeExtensions.SafeGesturesInsets(ComposableContext.Current);

    /// <summary>Reads the current status-bar insets.</summary>
    public static WindowInsets StatusBarsInsets() =>
        ComposeExtensions.StatusBarsInsets(ComposableContext.Current);

    /// <summary>Reads the union of status-bar and navigation-bar insets.</summary>
    public static WindowInsets SystemBarsInsets() =>
        ComposeExtensions.SystemBarsInsets(ComposableContext.Current);

    /// <summary>Reads the current system-gesture insets.</summary>
    public static WindowInsets SystemGesturesInsets() =>
        ComposeExtensions.SystemGesturesInsets(ComposableContext.Current);

    /// <summary>Reads insets where taps are intercepted by system UI.</summary>
    public static WindowInsets TappableElementInsets() =>
        ComposeExtensions.TappableElementInsets(ComposableContext.Current);

    /// <summary>Reads waterfall-display edge insets.</summary>
    public static WindowInsets WaterfallInsets() =>
        ComposeExtensions.WaterfallInsets(ComposableContext.Current);
}
