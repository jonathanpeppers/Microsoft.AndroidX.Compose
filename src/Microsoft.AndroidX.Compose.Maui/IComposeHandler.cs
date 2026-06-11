using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.Maui;

/// <summary>
/// Implemented by every handler in <c>Microsoft.AndroidX.Compose.Maui</c>
/// so that an enclosing Compose composition (rooted in
/// <see cref="Handlers.PageHandler"/>) can fold the handler's
/// rendering into <em>one</em> composition tree per page instead of
/// spinning up a separate <c>ComposeView</c> per handler.
/// </summary>
/// <remarks>
/// <para>Strictly internal — the consumer never sees Compose at the
/// MAUI surface. They call
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>
/// once and continue writing pure MAUI markup. The Compose facade
/// types (<see cref="ComposableNode"/>, <see cref="IComposer"/>) only
/// flow between our handlers and the <see cref="ComposeWalker"/>.</para>
///
/// <para>Implementors return a <see cref="ComposableNode"/> per call;
/// the walker inserts it under the active composer. Mutable state
/// owned by the handler (<see cref="MutableState{T}"/> slots written
/// by property mappers) is what makes the same node observe MAUI
/// property changes between recompositions.</para>
/// </remarks>
internal interface IComposeHandler
{
    /// <summary>
    /// Build the <see cref="ComposableNode"/> contributing this
    /// handler's virtual view into the enclosing composition.
    /// Called on the Compose composition thread inside
    /// <see cref="ComposeWalker.Render(Microsoft.Maui.IView, IComposer, IMauiContext)"/>.
    /// </summary>
    ComposableNode BuildNode(IComposer composer);

    /// <summary>
    /// Bump the per-handler view-properties version slot so the
    /// next composition pass re-reads the live <see cref="IView"/>
    /// transform / visibility / clip / shadow values via
    /// <see cref="Platform.ModifierBridge.ApplyViewProperties"/>.
    /// Called from <see cref="Hosting.AppHostBuilderExtensions.RemapForCompose"/>
    /// hooks installed on <c>ViewHandler.ViewMapper</c>.
    /// </summary>
    /// <remarks>
    /// MAUI's <see cref="IView"/> transform properties are all
    /// struct- or geometry-typed (<c>double</c> coords, <c>Visibility</c>
    /// enum, <c>IShape</c>, <c>IShadow</c>) — none of which fit
    /// <c>MutableState&lt;T&gt;</c>'s primitive-or-Java-peer
    /// constraint. The version-counter pattern (mirror of
    /// <see cref="Handlers.LayoutHandler.MapPadding"/>'s
    /// <c>_paddingVersion</c>) replaces a per-property slot with a
    /// single <c>MutableState&lt;int&gt;</c> bumped on any change;
    /// <see cref="ComposableNode"/>s subscribe by reading it inside
    /// <c>BuildNode</c> and live-read the value at composition time.
    /// </remarks>
    void BumpViewPropertiesVersion();
}
