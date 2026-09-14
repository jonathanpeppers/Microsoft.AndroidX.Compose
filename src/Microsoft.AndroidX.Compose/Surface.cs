namespace AndroidX.Compose;

/// <summary>
/// Material 3 non-interactive <c>Surface</c>. Applies background
/// color, elevation, and clipping to its content.
/// </summary>
/// <remarks>
/// Null styling properties use Material 3 defaults: the theme's surface color,
/// its matching content color, zero tonal/shadow elevation, and no border.
/// Supplied colors (including transparent packed zero) and zero elevations are
/// explicit values. Content inherits <c>ContentColor</c> unless it overrides
/// its own color. Tonal elevation accumulates through nested surfaces; shadow
/// elevation does not change layout or drawing order.
/// The generated composable method distinguishes omitted arguments from
/// explicit null: a null color/elevation lowers to zero, and a null border
/// means no border. Tree properties use null to request the Kotlin default.
/// Color defaults are resolved from the active Kotlin theme in a stable group
/// before the bound Surface call, preserving child state when omission changes.
/// </remarks>
public sealed partial class Surface
{
#if DEBUG
    internal static Action<Kotlin.Jvm.Functions.IFunction2, int, int, long, long, int>? ContentObserver { get; set; }
#endif
}
