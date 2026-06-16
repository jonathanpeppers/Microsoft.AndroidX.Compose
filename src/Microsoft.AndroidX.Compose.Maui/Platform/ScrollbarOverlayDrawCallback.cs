using AndroidX.Compose;
using Android.Graphics;
using Android.Runtime;
using Kotlin.Jvm.Functions;
using AndroidColor   = Android.Graphics.Color;
using AndroidPaint   = Android.Graphics.Paint;
using AndroidRectF   = Android.Graphics.RectF;
using ComposeBridges = AndroidX.Compose.ComposeBridges;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// <c>Function1&lt;DrawScope, Unit&gt;</c> adapter that paints an overlay
/// scrollbar thumb for <see cref="Handlers.ScrollViewHandler"/>. Compose
/// Foundation 1.11 ships no public scrollbar API
/// (<c>Modifier.verticalScroll</c> / <c>Modifier.horizontalScroll</c>
/// have no scrollbar-visibility flag, and <c>androidx.compose.foundation.v2</c>'s
/// <c>scrollbar</c> extension is desktop-only), so we draw a minimal
/// thumb pip directly into the native <see cref="Android.Graphics.Canvas"/>
/// from inside <see cref="ModifierExtensions.DrawBehind"/>.
/// </summary>
/// <remarks>
/// <para>Allocated <strong>once per <see cref="Handlers.ScrollViewHandler"/>
/// instance</strong> as a <c>readonly</c> field so the JNI peer stays
/// stable across recompositions — re-allocating the JCW per recomposition
/// would churn the JNI ref table without any payoff and rebuild the
/// underlying <c>DrawBehindElement</c> on every frame. The handler
/// mutates the public properties below before each recompose;
/// <see cref="Invoke"/> reads the latest values on each draw pass.</para>
///
/// <para>The captured <see cref="State"/> wraps a Compose
/// <c>ScrollState</c> whose <c>value</c> / <c>maxValue</c> /
/// <c>viewportSize</c> are themselves <c>MutableState</c>-backed, so
/// reading them inside this callback registers the snapshot
/// dependency and Compose redraws the thumb whenever the user (or a
/// programmatic <see cref="ScrollState.ScrollToAsync"/>) advances the
/// position. No manual recompose plumbing required.</para>
///
/// <para>Limitations vs. stock Android <c>ScrollView</c>:</para>
/// <list type="bullet">
///   <item><description>No auto-hide / fade animation — Compose has no
///   equivalent of <c>View.setScrollbarFadingEnabled</c>, so
///   <see cref="Microsoft.Maui.ScrollBarVisibility.Default"/> behaves
///   like <see cref="Microsoft.Maui.ScrollBarVisibility.Always"/> for
///   the lifetime of the page (the handler only suppresses the overlay
///   when the caller asks for <see cref="Microsoft.Maui.ScrollBarVisibility.Never"/>).</description></item>
///   <item><description>No track — only the thumb is drawn; the rest of the
///   overlay <see cref="Box"/> is fully transparent.</description></item>
///   <item><description>Thumb tint is a fixed
///   semi-transparent neutral gray, not theme-aware.</description></item>
/// </list>
///
/// <para>Public so it can be referenced from a <c>readonly</c> field
/// on the handler. Not part of the developer-facing API.</para>
/// </remarks>
[Register("net/compose/maui/ScrollbarOverlayDrawCallback")]
public sealed class ScrollbarOverlayDrawCallback : Java.Lang.Object, IFunction1
{
    /// <summary>Default thumb thickness, in DIPs (matches Material's small-scrollbar look).</summary>
    internal const float ThicknessDip = 4f;

    /// <summary>Minimum thumb length, in DIPs, so a tall content's tiny thumb fraction is still tappable.</summary>
    internal const float MinThumbLengthDip = 24f;

    /// <summary>Reused across draw passes — color/alpha set in <see cref="Invoke"/>.</summary>
    readonly AndroidPaint _paint = new(PaintFlags.AntiAlias);

    /// <summary>
    /// Live <see cref="ScrollState"/> wrapper. <c>Value</c> /
    /// <c>MaxValue</c> / <c>ViewportSize</c> are read on every draw
    /// frame. <see langword="null"/> while the handler is between
    /// recomposes.
    /// </summary>
    public ScrollState? State { get; set; }

    /// <summary>
    /// <see langword="true"/> for vertical scroll (thumb sits on the
    /// right edge), <see langword="false"/> for horizontal (thumb sits
    /// on the bottom edge).
    /// </summary>
    public bool Vertical { get; set; } = true;

    /// <summary>Display density in pixels-per-DIP, captured by the handler from <see cref="Android.Util.DisplayMetrics.Density"/>.</summary>
    public float Density { get; set; } = 1f;

    /// <summary>Thumb fill colour as packed <c>0xAARRGGBB</c>. Defaults to a semi-transparent neutral gray.</summary>
    public int ThumbArgb { get; set; } = unchecked((int)0x66808080);

    /// <summary>
    /// Kotlin <c>Function1.invoke</c> entry point. <paramref name="p0"/>
    /// is the Compose <c>DrawScope</c> the runtime hands back inside
    /// <c>Modifier.drawBehind</c>.
    /// </summary>
    public Java.Lang.Object? Invoke(Java.Lang.Object? p0)
    {
        var unit = Kotlin.Unit.Instance
            ?? throw new InvalidOperationException("Kotlin.Unit.Instance not available.");

        if (p0 is null)
            return unit;

        var state = State;
        if (state is null || Density <= 0f)
            return unit;

        // ScrollState.maxValue starts at int.MaxValue before layout has
        // run; treat that and zero as "no thumb yet".
        int maxValue = state.MaxValue;
        if (maxValue <= 0 || maxValue == int.MaxValue)
            return unit;

        int viewportPx = state.ViewportSize;
        if (viewportPx <= 0)
            return unit;

        long packedSize = ComposeBridges.DrawScopeGetSize(p0.Handle);
        float widthPx  = ComposeBridges.UnpackSizeWidth(packedSize);
        float heightPx = ComposeBridges.UnpackSizeHeight(packedSize);
        if (widthPx <= 0f || heightPx <= 0f)
            return unit;

        var nativeCanvas = ComposeBridges.DrawScopeGetNativeCanvas(p0.Handle);
        if (nativeCanvas is null)
            return unit;

        _paint.SetStyle(AndroidPaint.Style.Fill);
        _paint.Color = new AndroidColor(ThumbArgb);

        // The overlay `Box` is sized at exactly the bar thickness, so
        // `widthPx` (vertical) or `heightPx` (horizontal) is the
        // perpendicular axis; the thumb fills it. The other axis is the
        // track length.
        float trackPx       = Vertical ? heightPx : widthPx;
        float minThumbLenPx = MinThumbLengthDip * Density;

        // Thumb length: fraction of viewport over total scrollable
        // content, clamped to a tappable minimum. Compose's
        // `ScrollState.maxValue` is the largest distance the content
        // can be scrolled (i.e. content length minus viewport), so
        // total content length = viewport + maxValue.
        float contentLenPx = viewportPx + maxValue;
        float thumbLenPx   = trackPx * (viewportPx / contentLenPx);
        if (thumbLenPx < minThumbLenPx)
            thumbLenPx = minThumbLenPx;
        if (thumbLenPx > trackPx)
            thumbLenPx = trackPx;

        float trackUsablePx = trackPx - thumbLenPx;
        if (trackUsablePx < 0f)
            trackUsablePx = 0f;
        float thumbStartPx = trackUsablePx * ((float)state.Value / maxValue);

        // Capsule-shaped thumb — rounding the corners to the full
        // thickness gives a pill shape on tall content and a near-circle
        // when the thumb collapses to its minimum tappable length.
        float radiusPx = Vertical ? widthPx / 2f : heightPx / 2f;
        var rect = Vertical
            ? new AndroidRectF(0f, thumbStartPx, widthPx, thumbStartPx + thumbLenPx)
            : new AndroidRectF(thumbStartPx, 0f, thumbStartPx + thumbLenPx, heightPx);
        try
        {
            nativeCanvas.DrawRoundRect(rect, radiusPx, radiusPx, _paint);
        }
        finally
        {
            rect.Dispose();
        }
        return unit;
    }
}
