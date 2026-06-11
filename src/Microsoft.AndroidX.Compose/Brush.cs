using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// C# wrapper around Kotlin's <c>androidx.compose.ui.graphics.Brush</c>
/// — the value passed to fill / stroke slots that want richer paint
/// than a single <see cref="Color"/>.
/// </summary>
/// <remarks>
/// <para>
/// Build a <see cref="Brush"/> via the static factory methods:
/// <see cref="SolidColor(Color)"/>,
/// <see cref="LinearGradient(Color[], Offset, Offset, TileMode)"/>,
/// <see cref="HorizontalGradient(Color[], float, float, TileMode)"/>,
/// <see cref="VerticalGradient(Color[], float, float, TileMode)"/>,
/// <see cref="RadialGradient(Color[], Offset, float, TileMode)"/>, and
/// <see cref="SweepGradient(Color[], Offset)"/>.
/// </para>
/// <para>
/// Pass the returned instance to <c>Modifier.Background(brush)</c> /
/// <c>Modifier.Background(brush, shape)</c> /
/// <c>Modifier.Border(width, brush)</c> / etc. Brush instances are
/// immutable — keep one in a field (or
/// <c>composer.Remember(() =&gt; Brush.HorizontalGradient(...))</c>)
/// when the same brush renders every frame; otherwise let the
/// composition build a fresh instance each pass.
/// </para>
/// </remarks>
public class Brush : Java.Lang.Object
{
    private protected Brush(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    /// <summary>
    /// <c>SolidColor(color)</c> — a <see cref="Brush"/> that paints a
    /// single flat color. Mostly useful for APIs that demand a
    /// <c>Brush</c> rather than a <see cref="Color"/>; for the typical
    /// "fill with a color" case prefer the
    /// <c>Modifier.Background(Color)</c> overload, which avoids the
    /// extra JNI allocation.
    /// </summary>
    public static Brush SolidColor(Color color)
    {
        IntPtr handle = ComposeBridges.BrushSolidColor(color);
        return new Brush(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>Brush.linearGradient(colors, start, end, tileMode)</c> —
    /// gradient interpolating along the line from
    /// <paramref name="start"/> to <paramref name="end"/>. Colors are
    /// spaced evenly along the line; pass
    /// <see cref="Offset.Zero"/> and a far-away <see cref="Offset"/>
    /// for endpoint pinning, or
    /// <c>(Offset.Zero, new Offset(0f, Single.PositiveInfinity))</c>
    /// for a fill-the-whole-vertical-axis gradient (matches Kotlin's
    /// default of <c>Offset(0f, Float.POSITIVE_INFINITY)</c>).
    /// </summary>
    public static Brush LinearGradient(
        Color[] colors,
        Offset start,
        Offset end,
        TileMode tileMode = TileMode.Clamp)
    {
        ArgumentNullException.ThrowIfNull(colors);
        long[] packed = PackColors(colors);
        IntPtr handle = ComposeBridges.BrushLinearGradient(
            packed, start.Packed, end.Packed, (int)tileMode);
        return new Brush(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// Convenience overload — <c>LinearGradient(colors, Offset.Zero,
    /// new Offset(0f, Single.PositiveInfinity), Clamp)</c>. Produces a
    /// top-to-bottom vertical fade matching Kotlin's
    /// <c>Brush.linearGradient(colors)</c> default.
    /// </summary>
    public static Brush LinearGradient(params Color[] colors) =>
        LinearGradient(colors, Offset.Zero, new Offset(0f, float.PositiveInfinity), TileMode.Clamp);

    /// <summary>
    /// <c>Brush.horizontalGradient(colors, startX, endX, tileMode)</c>
    /// — gradient along the X axis. The Y component spans the full
    /// height of whatever box this brush is painted onto.
    /// </summary>
    public static Brush HorizontalGradient(
        Color[] colors,
        float startX = 0f,
        float endX = float.PositiveInfinity,
        TileMode tileMode = TileMode.Clamp)
    {
        ArgumentNullException.ThrowIfNull(colors);
        long[] packed = PackColors(colors);
        IntPtr handle = ComposeBridges.BrushHorizontalGradient(
            packed, startX, endX, (int)tileMode);
        return new Brush(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>Brush.horizontalGradient(colors)</c> — Kotlin default
    /// (full-width sweep, <see cref="TileMode.Clamp"/>).
    /// </summary>
    public static Brush HorizontalGradient(params Color[] colors) =>
        HorizontalGradient(colors, 0f, float.PositiveInfinity, TileMode.Clamp);

    /// <summary>
    /// <c>Brush.verticalGradient(colors, startY, endY, tileMode)</c>
    /// — gradient along the Y axis. The X component spans the full
    /// width of whatever box this brush is painted onto.
    /// </summary>
    public static Brush VerticalGradient(
        Color[] colors,
        float startY = 0f,
        float endY = float.PositiveInfinity,
        TileMode tileMode = TileMode.Clamp)
    {
        ArgumentNullException.ThrowIfNull(colors);
        long[] packed = PackColors(colors);
        IntPtr handle = ComposeBridges.BrushVerticalGradient(
            packed, startY, endY, (int)tileMode);
        return new Brush(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>Brush.verticalGradient(colors)</c> — Kotlin default
    /// (full-height sweep, <see cref="TileMode.Clamp"/>).
    /// </summary>
    public static Brush VerticalGradient(params Color[] colors) =>
        VerticalGradient(colors, 0f, float.PositiveInfinity, TileMode.Clamp);

    /// <summary>
    /// <c>Brush.radialGradient(colors, center, radius, tileMode)</c>
    /// — gradient radiating outward from <paramref name="center"/> to
    /// <paramref name="radius"/>. Pass <see cref="Offset.Unspecified"/>
    /// for <paramref name="center"/> to centre the gradient on the
    /// painted box.
    /// </summary>
    public static Brush RadialGradient(
        Color[] colors,
        Offset center,
        float radius = float.PositiveInfinity,
        TileMode tileMode = TileMode.Clamp)
    {
        ArgumentNullException.ThrowIfNull(colors);
        long[] packed = PackColors(colors);
        IntPtr handle = ComposeBridges.BrushRadialGradient(
            packed, center.Packed, radius, (int)tileMode);
        return new Brush(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>Brush.radialGradient(colors)</c> — auto-centred,
    /// auto-sized, <see cref="TileMode.Clamp"/>.
    /// </summary>
    public static Brush RadialGradient(params Color[] colors) =>
        RadialGradient(colors, Offset.Unspecified, float.PositiveInfinity, TileMode.Clamp);

    /// <summary>
    /// <c>Brush.sweepGradient(colors, center)</c> — angular gradient
    /// that wraps around <paramref name="center"/> through 360°. Pass
    /// <see cref="Offset.Unspecified"/> for <paramref name="center"/>
    /// to centre on the painted box.
    /// </summary>
    public static Brush SweepGradient(Color[] colors, Offset center)
    {
        ArgumentNullException.ThrowIfNull(colors);
        long[] packed = PackColors(colors);
        IntPtr handle = ComposeBridges.BrushSweepGradient(packed, center.Packed);
        return new Brush(handle, JniHandleOwnership.TransferLocalRef);
    }

    /// <summary>
    /// <c>Brush.sweepGradient(colors)</c> — auto-centred angular
    /// gradient sweeping through 360°.
    /// </summary>
    public static Brush SweepGradient(params Color[] colors) =>
        SweepGradient(colors, Offset.Unspecified);

    static long[] PackColors(Color[] colors)
    {
        if (colors.Length == 0)
            throw new ArgumentException(
                "Gradient must have at least one color stop.", nameof(colors));
        long[] packed = new long[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            packed[i] = colors[i];
        return packed;
    }
}
