using global::AndroidX.Compose.Material3;
using global::AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// Material 3 <c>HorizontalDivider</c> — a thin horizontal line used to
/// separate sibling content blocks (typically inside a
/// <see cref="Column"/>).
/// </summary>
public sealed class HorizontalDivider : ComposableNode
{
    /// <summary>Optional explicit thickness in Dp. Leave null to use the Material default.</summary>
    public float? ThicknessDp { get; set; }

    /// <summary>Optional explicit ARGB color (packed into a Compose <c>Color</c> long). Leave null to use the Material default.</summary>
    public long? ColorArgb { get; set; }

    public override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        int defaults = (int)HorizontalDividerDefault.All;
        if (modifier is not null)        defaults &= ~(int)HorizontalDividerDefault.Modifier;
        if (ThicknessDp.HasValue)        defaults &= ~(int)HorizontalDividerDefault.Thickness;
        if (ColorArgb.HasValue)          defaults &= ~(int)HorizontalDividerDefault.Color;

        DividerKt.HorizontalDivider(
            modifier:  modifier,
            thickness: ThicknessDp ?? 0f,
            color:     ColorArgb   ?? 0L,
            _composer: composer,
            p4:        0,
            _changed:  defaults);
    }
}
