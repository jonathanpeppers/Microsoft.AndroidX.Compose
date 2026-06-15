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

        if (handler is IComposeHandler compose)
        {
            return compose.BuildNode(composer);
        }

        // Stock-handler fallback: host the platform view inside Compose's
        // `AndroidView { … }` interop. We have to apply a Compose
        // `Modifier.Size` (or `Width`/`Height`) from the MAUI virtual
        // view's `WidthRequest`/`HeightRequest` ourselves — without it
        // Compose hands the AndroidView an unbounded slot and the hosted
        // Android `View` measures wrap-content. For self-drawing MAUI
        // views with no intrinsic size (`MauiShapeView`,
        // `PlatformGraphicsView`) that collapses to 0×0 and the canvas
        // paints nothing even though MAUI's mapper (`ShapeViewHandler`,
        // `GraphicsViewHandler`) ran during `ToPlatform()` and set up a
        // `Drawable`. Compose-aware handlers (`BoxViewHandler`,
        // `LabelHandler`, …) apply this themselves via
        // `IComposeHandler.BuildNode`; the fallback covers everyone else.
        Modifier? modifier = null;
        if (view is Microsoft.Maui.Controls.VisualElement ve)
        {
            modifier = (ve.WidthRequest, ve.HeightRequest) switch
            {
                ( >= 0d, >= 0d ) => Modifier.Size(new Dp((float)ve.WidthRequest), new Dp((float)ve.HeightRequest)),
                ( >= 0d, _ ) => Modifier.Width(new Dp((float)ve.WidthRequest)),
                ( _, >= 0d ) => Modifier.FillMaxWidth().Height(new Dp((float)ve.HeightRequest)),
                _ => null,
            };
        }

        return new AndroidView(factory: _ => view.ToPlatform(mauiContext))
        {
            Modifier = modifier,
        };
    }
}
