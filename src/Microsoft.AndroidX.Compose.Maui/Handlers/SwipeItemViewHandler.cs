using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// Compose-backed handler for a MAUI <see cref="ISwipeItemView"/>.
/// Its custom content is folded into the owning
/// <see cref="SwipeViewHandler"/>'s action panel.
/// </summary>
public partial class SwipeItemViewHandler : ComposeElementHandler<ISwipeItemView>
{
    /// <summary>Property mapper for custom swipe-item content and visibility.</summary>
    public static IPropertyMapper<ISwipeItemView, SwipeItemViewHandler> Mapper =
        new PropertyMapper<ISwipeItemView, SwipeItemViewHandler>(ViewHandler.ViewMapper)
        {
            ["Content"]    = MapContent,
            ["Visibility"] = MapVisibility,
        };

    /// <summary>Command mapper inheriting the standard view commands.</summary>
    public static CommandMapper<ISwipeItemView, SwipeItemViewHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<int> _contentVersion = new(0);

    /// <summary>Construct a handler with the default mappers.</summary>
    public SwipeItemViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public SwipeItemViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        _ = _contentVersion.Value;
        SubscribeToViewProperties();

        var view = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on SwipeItemViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on SwipeItemViewHandler.");

        var box = new Box
        {
            Modifier = Modifier.Companion
                .ApplyViewProperties(view)
                .ApplySemantics(view),
        };
        if (view.PresentedContent is { } content)
            box.Add(c => ComposeWalker.Render(content, c, context));
        return box;
    }

    /// <summary>Recompose when custom content changes.</summary>
    public static void MapContent(SwipeItemViewHandler handler, ISwipeItemView _) =>
        handler._contentVersion.Value++;

    /// <summary>Recompose when item visibility changes.</summary>
    public static void MapVisibility(SwipeItemViewHandler handler, ISwipeItemView _) =>
        handler._contentVersion.Value++;
}
