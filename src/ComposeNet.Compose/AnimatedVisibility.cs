using AndroidX.Compose.Animation;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// <c>AnimatedVisibility</c> — animates the appearance and disappearance
/// of its content as <see cref="Visible"/> toggles. The default
/// transitions fade and expand/shrink the content; v1 uses Compose's
/// defaults rather than exposing the
/// <c>EnterTransition</c> / <c>ExitTransition</c> factory functions
/// (<c>fadeIn()</c>, <c>slideInVertically()</c>, etc.) — wire those up
/// in a follow-up once the animation transition factories are exposed
/// in the binding.
/// </summary>
/// <remarks>
/// Children are rendered through Compose's <c>AnimatedVisibilityScope</c>
/// receiver. The scope's helpers (<c>Modifier.animateEnterExit</c>) are
/// not exposed in v1 — children render with the discarded scope.
/// Mirror of the bound
/// <see cref="AnimatedVisibilityKt.AnimatedVisibility(bool, AndroidX.Compose.UI.IModifier?, EnterTransition?, ExitTransition?, string?, Kotlin.Jvm.Functions.IFunction3, IComposer?, int, int)"/>
/// overload — no JNI bridge, every parameter except <c>visible</c> and
/// <c>content</c> defaults to Kotlin's value.
/// </remarks>
public sealed class AnimatedVisibility : ComposableContainer
{
    readonly bool _visible;

    /// <summary>Build an AnimatedVisibility container with the given visibility.</summary>
    /// <param name="visible">When <see langword="true"/> the content is shown (entering if previously hidden); when <see langword="false"/> the content is hidden (exiting if previously shown).</param>
    public AnimatedVisibility(bool visible) => _visible = visible;

    /// <summary>Current visibility passed to the underlying Compose <c>AnimatedVisibility</c>.</summary>
    public bool Visible => _visible;

    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier();
        var content  = ComposableLambdas.Wrap3(composer, RenderChildren);

        // $default bit positions (Kotlin source order):
        //   0 = visible        (always provided → cleared)
        //   1 = modifier       (cleared when caller set it)
        //   2 = enter          (left set → use Compose default)
        //   3 = exit           (left set → use Compose default)
        //   4 = label          (left set → use Compose default)
        //   5 = content        (always provided → cleared)
        int defaults = (1 << 2) | (1 << 3) | (1 << 4);
        if (modifier is null) defaults |= 1 << 1;

        AnimatedVisibilityKt.AnimatedVisibility(
            visible:   _visible,
            modifier:  modifier,
            enter:     null,
            exit:      null,
            label:     null,
            content:   content,
            _composer: composer,
            p7:        0,           // $changed
            _changed:  defaults);   // $default
    }
}
