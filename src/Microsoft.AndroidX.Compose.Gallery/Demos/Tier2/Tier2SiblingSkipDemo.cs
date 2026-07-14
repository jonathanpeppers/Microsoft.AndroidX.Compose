using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.Runtime;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.Tier2;

/// <summary>
/// Tier 2 proof demo. Two sibling <c>[Composable]</c> static methods
/// render side by side. A <see cref="MutableNumberState{T}"/> ticks
/// on every button tap, and only the <c>Ticking</c> sibling reads
/// it — the <c>Static</c> sibling's parameter is a constant.
/// </summary>
/// <remarks>
/// <para>
/// Each sibling increments an in-process execution counter the first
/// thing inside its body and renders both the input value and the
/// counter. Tapping the button reruns the parent composable; the
/// runtime then invokes each sibling's interceptor, which diffs its
/// parameters via <see cref="ComposeExtensions.DiffSlot{T}"/>:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>Ticking</c> receives a new <c>int</c> value each pass →
///     interceptor reads <c>Different</c> → body runs → exec counter
///     increments.
///   </description></item>
///   <item><description>
///     <c>Static</c> receives the same constant string each pass →
///     interceptor reads <c>Same</c> → body is skipped via
///     <c>SkipToGroupEnd</c> → exec counter stays flat.
///   </description></item>
/// </list>
/// <para>
/// That divergence is the runtime proof Tier 2 works — without the
/// generator's restart group + DiffSlot + skip path, both counters
/// would tick together.
/// </para>
/// </remarks>
public static class Tier2SiblingSkipDemo
{
    static int s_tickingExec;
    static int s_staticExec;

    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "tier2-sibling-skip",
        CategoryId:  "tier2",
        Title:       "[Composable] sibling skip",
        Description: "Two sibling Tier 2 composables side by side; only one reads the ticking state. The other's exec counter stays flat — runtime proof the skip path fires.",
        Build:       static c =>
        {
            var tick = c.Remember(() => new MutableNumberState<int>(0));
            return new Tier2Adapter(comp => Parent(comp, tick.Value, () => tick.Value++));
        });

    /// <summary>
    /// Parent <c>[Composable]</c> hosting both siblings and the button
    /// that drives the ticking state.
    /// </summary>
    [Composable]
    public static void Parent(IComposer composer, int tickCount, Action onTick)
    {
        Column(composer, c =>
        {
            // The static sibling receives a literal string that never
            // changes from one pass to the next, so its interceptor's
            // DiffSlot reads `Same` and the body is skipped.
            Static(c, "constant input — should never re-run");

            // The ticking sibling reads the live `tickCount`. Its
            // interceptor's DiffSlot reads `Different` whenever the
            // user taps the button, so the body runs every pass.
            Ticking(c, tickCount);

            Button(c, onTick, cc => Text(cc, "Tap to tick"));
        });
    }

    /// <summary>
    /// Tier 2 sibling whose only parameter is a constant string. Its
    /// generator-emitted interceptor should detect the unchanged input
    /// and call <c>SkipToGroupEnd</c> instead of running this body.
    /// </summary>
    [Composable]
    public static void Static(IComposer composer, string label)
    {
        s_staticExec++;
        Text(composer, $"Static side: \"{label}\" — exec={s_staticExec}");
    }

    /// <summary>
    /// Tier 2 sibling that reads the ticking <see cref="MutableNumberState{T}"/>.
    /// Its interceptor's DiffSlot reads <c>Different</c> on every tap
    /// so the body runs every pass and the exec counter tracks the
    /// tap count exactly.
    /// </summary>
    [Composable]
    public static void Ticking(IComposer composer, int tickCount)
    {
        s_tickingExec++;
        Text(composer, $"Ticking side: count={tickCount}, exec={s_tickingExec}");
    }
}
