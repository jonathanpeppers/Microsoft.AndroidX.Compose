using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.Maui.Platform;

namespace Microsoft.AndroidX.Compose.Maui;

/// <summary>
/// Bridges MAUI's <see cref="IView"/> tree into the active Compose
/// composition during a render pass. The single
/// <see cref="ComposeView"/> rooted in
/// <see cref="Handlers.PageHandler"/> calls
/// <see cref="Render(IView, IComposer, IMauiContext)"/> per child;
/// container handlers
/// (<see cref="Handlers.LayoutHandler"/>,
/// <see cref="Handlers.ScrollViewHandler"/>) call it from inside
/// their own composables to recurse.
/// </summary>
/// <remarks>
/// <para>Two paths:</para>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="IComposeHandler"/> → fold the handler's render
///       into the current composition via
///       <see cref="IComposeHandler.BuildNode(IComposer)"/>. Zero
///       extra <c>ComposeView</c>s, zero extra recomposers.
///     </description>
///   </item>
///   <item>
///     <description>
///       Anything else (stock handler, custom renderer,
///       not-yet-converted control) → wrap via
///       <see cref="AndroidView"/>. MAUI's normal handler resolution
///       still produces a stock Android view; Compose hosts it as an
///       <c>AndroidView</c> in the same composition.
///     </description>
///   </item>
/// </list>
/// </remarks>
internal static class ComposeWalker
{
    /// <summary>
    /// Materialise a <see cref="ComposableNode"/> for
    /// <paramref name="view"/>, dispatching on whether its handler is
    /// Compose-aware.
    /// </summary>
    public static ComposableNode Render(IView view, IComposer composer, IMauiContext mauiContext)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(mauiContext);

        // The handler may be null on the very first render — MAUI hasn't
        // resolved one yet for this child. ToHandler() forces resolution
        // and creates the handler on demand. For the IComposeHandler
        // path we need a handler before BuildNode; for the AndroidView
        // fallback the factory lambda calls ToPlatform() which does the
        // same thing.
        var handler = view.Handler;
        if (handler is null)
        {
            _ = view.ToHandler(mauiContext);
            handler = view.Handler;
        }

        return handler is IComposeHandler compose
            ? compose.BuildNode(composer)
            : new AndroidView(_ => view.ToPlatform(mauiContext));
    }
}
