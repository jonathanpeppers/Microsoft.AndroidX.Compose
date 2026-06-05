using Android.Runtime;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI.Graphics.Vector;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Icon</c> — renders a small image used for affordance,
/// typically inside a <see cref="Button"/>, <see cref="IconButton"/>,
/// or a <see cref="NavigationBarItem"/>.
///
/// Two source types are supported:
/// <list type="bullet">
/// <item><description>An <see cref="ImageVector"/> obtained from a
/// Compose icon library — uses the directly-bound
/// <c>IconKt.Icon(ImageVector, ...)</c> overload.</description></item>
/// <item><description>An Android drawable resource id — resolved
/// inside <see cref="Render"/> via <c>painterResource(id)</c> and then
/// drawn through a JNI bridge to the stripped
/// <c>IconKt.Icon-ww6aTOc(Painter, ...)</c> overload.</description></item>
/// </list>
/// </summary>
public sealed class Icon : ComposableNode
{
    readonly ImageVector? _vector;
    readonly int? _resourceId;
    readonly string? _contentDescription;

    /// <summary>
    /// Optional ARGB color packed into a Compose <c>Color</c> long.
    /// Leave null to inherit the surrounding Material content color.
    /// </summary>
    public long? TintArgb { get; set; }

    /// <summary>Render an <see cref="ImageVector"/> (e.g. from a Compose icon library).</summary>
    public Icon(ImageVector imageVector, string? contentDescription)
    {
        _vector = imageVector;
        _contentDescription = contentDescription;
    }

    /// <summary>Render an Android drawable resource (resolved via <c>painterResource</c>).</summary>
    public Icon(int drawableResourceId, string? contentDescription)
    {
        _resourceId = drawableResourceId;
        _contentDescription = contentDescription;
    }

    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        // bit 0 (imageVector / painter) and bit 1 (contentDescription)
        // bit 0 (imageVector / painter) is always supplied. bit 1
        // (contentDescription) is *also* always supplied — we forward
        // the caller's value through, including null. Compose treats
        // contentDescription = null as "decorative icon" (no semantics
        // node), which is meaningfully different from an empty string,
        // so callers can opt in by leaving the parameter unset.
        int defaults = (int)IconDefault.All
                       & ~(int)IconDefault.ImageVector
                       & ~(int)IconDefault.ContentDescription;
        if (modifier is not null) defaults &= ~(int)IconDefault.Modifier;
        if (TintArgb.HasValue)    defaults &= ~(int)IconDefault.Tint;

        long tint = TintArgb ?? 0L;

        if (_vector is not null)
        {
            IconKt.Icon(
                imageVector:        _vector,
                contentDescription: _contentDescription!,
                modifier:           modifier,
                tint:               tint,
                _composer:          composer,
                p5:                 0,
                _changed:           defaults);
            return;
        }

        // Painter overload — resolve the drawable inside the composition
        // (painterResource is itself @Composable) and forward via JNI.
        // The bridge enum (`IconPainterDefault`) has the same bit layout
        // as `IconDefault` for the user-visible bits (ContentDescription,
        // Modifier, Tint at bits 1-3), so the same `defaults` mask is
        // valid for either path.
        IntPtr painterRef = ComposeBridges.PainterResource(_resourceId!.Value, composer);
        try
        {
            ComposeBridges.IconPainter(
                painter:            painterRef,
                contentDescription: _contentDescription,
                modifier:           modifier,
                tint:               tint,
                defaults:           defaults,
                composer:           composer);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(painterRef);
        }
    }
}
