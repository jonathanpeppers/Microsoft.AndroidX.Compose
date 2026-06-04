using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ModalWideNavigationRail</c> — the modal-overlay variant
/// of <see cref="WideNavigationRail"/>. Drawn on top of the host screen
/// with a scrim, suitable for narrow-width adaptive layouts. Stripped
/// from the binding because <c>collapsedShadowElevation</c> is a
/// <c>Dp</c> <c>@JvmInline value class</c> (hashed JVM name
/// <c>-k3FuEkE</c>); reached via <see cref="ComposeBridges"/>.
///
/// <para>The Kotlin signature has no <c>onDismissRequest</c> callback —
/// dismissal is driven internally by the rail's
/// <c>WideNavigationRailState</c>. Because that state's
/// <c>expand</c>/<c>collapse</c>/<c>snapTo</c> methods are Kotlin
/// suspend functions, and the facade does not have a
/// <c>LaunchedEffect</c>/coroutine-scope story yet, this facade ships
/// with two practical limitations callers should know about:</para>
/// <list type="bullet">
///   <item>The rail always remembers its state with
///   <c>WideNavigationRailValue.Expanded</c>, so it opens immediately
///   when mounted.</item>
///   <item>When the user dismisses by tapping the scrim, the rail
///   visually disappears (because <c>hideOnCollapse=true</c>) but the
///   C# facade has no way to observe the collapse. Drive close gestures
///   from your own <see cref="MutableState{T}"/>-of-<see cref="bool"/>
///   gate by mirroring the <see cref="ModalBottomSheet"/> pattern:
///   wrap the instance in a ternary and add a button inside the rail
///   content that flips the gate.</item>
/// </list>
///
/// <code>
/// var show = Remember(() =&gt; new MutableState&lt;bool&gt;(false));
/// new Row
/// {
///     new Button(onClick: () =&gt; show.Value = true) { new Text("Menu") },
///     show.Value
///         ? new ModalWideNavigationRail
///         {
///             Header = new Text("Sections"),
///             new WideNavigationRailItem(selected: tab == 0, onClick: () =&gt; { tab.Value = 0; show.Value = false; })
///             {
///                 Icon  = new Text("🏠"),
///                 Label = new Text("Home"),
///             },
///         }
///         : null,
/// };
/// </code>
/// </summary>
public sealed class ModalWideNavigationRail : ComposableContainer
{
    /// <summary>Optional header slot drawn at the top of the rail (e.g. a logo or menu button).</summary>
    public ComposableNode? Header { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Children.Count == 0)
            throw new System.InvalidOperationException(
                "ModalWideNavigationRail requires at least one child (the content slot has no Kotlin default).");

        // Bound C# call — RememberWideNavigationRailState is NOT stripped.
        // initialValue=Expanded so the rail opens immediately on mount;
        // the visibility-toggle pattern (see XML doc) controls mount/unmount.
        // _changed=0 because we provide initialValue.
        var stateObj = WideNavigationRailStateKt.RememberWideNavigationRailState(
            initialValue: WideNavigationRailValue.Expanded,
            _composer:    composer,
            p2:           0,
            _changed:     0);
        var stateHandle = ((Java.Lang.Object)stateObj).Handle;

        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        var headerNode = Header;
        var header = headerNode is null ? null
            : ComposableLambdas.Wrap2(composer, c => headerNode.Render(c));

        // Start from "default everything" and clear the bit for each
        // optional slot the user actually supplied. `state` and `content`
        // are not enum members (always provided), so they're not in `All`.
        // `hideOnCollapse` IS an enum member and we always pass true, so
        // we always clear its bit.
        int defaults = (int)ModalWideNavigationRailDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)ModalWideNavigationRailDefault.Modifier;
        defaults &= ~(int)ModalWideNavigationRailDefault.HideOnCollapse;
        if (header   is not null) defaults &= ~(int)ModalWideNavigationRailDefault.Header;

        ComposeBridges.ModalWideNavigationRail(
            modifier:        modifier,
            state:           stateHandle,
            hideOnCollapse:  true,
            header:          header,
            content:         content,
            defaults:        defaults,
            composer:        composer);
    }
}
