using Android.Runtime;
using BoundFont = AndroidX.Compose.UI.Text.Font;

namespace AndroidX.Compose;

/// <summary>Creates Compose fonts using the official AndroidX text binding.</summary>
public static class Font
{
    /// <summary>
    /// Describes a bundled Android font resource, with normal weight, upright style,
    /// and blocking loading unless overridden.
    /// </summary>
    /// <remarks>
    /// Supply a <c>Resource.Font</c> identifier for a TTF, OTF, or supported font XML
    /// resource. Resource existence and decoding are checked by Android when Compose
    /// resolves the font, not when this descriptor is created. Weight and style describe
    /// the font file for matching; they do not change its glyphs.
    /// The returned bound peer belongs to the caller. A family retains the Java font
    /// independently; disposing the descriptor afterward does not dispose the family.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The resource ID is not positive or the strategy is unknown.</exception>
    /// <exception cref="ObjectDisposedException">A supplied weight or style has been disposed.</exception>
    public static BoundFont.IFont Resource(
        int resourceId,
        FontWeight? weight = null,
        FontStyle? style = null,
        FontLoadingStrategy loadingStrategy = FontLoadingStrategy.Blocking)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resourceId);
        if (!Enum.IsDefined(loadingStrategy))
            throw new ArgumentOutOfRangeException(nameof(loadingStrategy), loadingStrategy, "Unknown font loading strategy.");

        weight ??= FontWeight.Normal;
        style ??= FontStyle.Normal;
        ObjectDisposedException.ThrowIf(weight.Handle == IntPtr.Zero, weight);
        ObjectDisposedException.ThrowIf(style.Handle == IntPtr.Zero, style);

        // These conversions may return shared peers; they must not be disposed here.
        var boundWeight = Java.Lang.Object.GetObject<BoundFont.FontWeight>(weight.Handle, JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException("FontWeight binding unavailable in Font.Resource.");
        var boundStyle = Java.Lang.Object.GetObject<BoundFont.FontStyle>(style.Handle, JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException("FontStyle binding unavailable in Font.Resource.");
        return BoundFont.FontKt.Font(resourceId, boundWeight, boundStyle.Value, (int)loadingStrategy)
            ?? throw new InvalidOperationException("AndroidX returned no font from Font.Resource.");
    }
}
