using AndroidX.Compose.Material3;

namespace ComposeNet;

/// <summary>
/// Caller-supplied state holder for <see cref="ModalNavigationDrawer"/>
/// and <see cref="DismissibleNavigationDrawer"/>. Wraps Kotlin's
/// <c>DrawerState</c> (created via <c>rememberDrawerState</c>) so a
/// facade can carry an initial <see cref="DrawerValue"/> across
/// recompositions and expose the live drawer position back to C#.
/// </summary>
/// <remarks>
/// <para>This type is named with a <c>Holder</c> suffix to avoid
/// colliding with the binding's
/// <see cref="AndroidX.Compose.Material3.DrawerState"/> class — both
/// would otherwise resolve to <c>DrawerState</c> when a user imports
/// <c>using AndroidX.Compose.Material3;</c> and
/// <c>using ComposeNet;</c> at the same time.</para>
/// <para>Construct one inside <c>Remember</c> so the same instance
/// survives recomposition:</para>
/// <code>
/// var drawer = Remember(() =&gt; new DrawerStateHolder(DrawerValue.Closed));
///
/// new ModalNavigationDrawer(state: drawer)
/// {
///     Drawer  = new ModalDrawerSheet { … },
///     Content = new Column { … },
///     ConfirmStateChange = v =&gt; !formIsDirty || v != DrawerValue.Closed,
/// };
/// </code>
/// </remarks>
public sealed class DrawerStateHolder
{
    internal DrawerState? Jvm;

    /// <summary>
    /// Initial <see cref="DrawerValue"/> the drawer remembers on first
    /// composition. Defaults to <see cref="DrawerValue.Closed"/>.
    /// </summary>
    public DrawerValue InitialValue { get; }

    /// <summary>
    /// Construct a holder with the given <paramref name="initialValue"/>
    /// (or <see cref="DrawerValue.Closed"/> when omitted).
    /// </summary>
    public DrawerStateHolder(DrawerValue? initialValue = null)
    {
        InitialValue = initialValue ?? DrawerValue.Closed!;
    }

    /// <summary>
    /// The drawer's current visual state. Falls back to
    /// <see cref="InitialValue"/> until the holder is bound to a live
    /// peer (i.e. the first render has happened).
    /// </summary>
    public DrawerValue CurrentValue => Jvm?.CurrentValue ?? InitialValue;

    /// <summary>
    /// The drawer's target state during animation, or the resting
    /// state otherwise. Falls back to <see cref="InitialValue"/> until
    /// bound.
    /// </summary>
    public DrawerValue TargetValue => Jvm?.TargetValue ?? InitialValue;

    /// <summary>
    /// <c>true</c> when the drawer is fully open. Equivalent to
    /// <c>CurrentValue == DrawerValue.Open</c>.
    /// </summary>
    public bool IsOpen => Jvm?.IsOpen ?? (InitialValue == DrawerValue.Open);

    /// <summary>
    /// <c>true</c> when the drawer is fully closed. Equivalent to
    /// <c>CurrentValue == DrawerValue.Closed</c>.
    /// </summary>
    public bool IsClosed => Jvm?.IsClosed ?? (InitialValue == DrawerValue.Closed);
}
