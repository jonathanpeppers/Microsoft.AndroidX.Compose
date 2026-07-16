using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor      = AndroidX.Compose.Color;
using MauiVisualElement = Microsoft.Maui.Controls.VisualElement;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.ContentView"/> handler that
/// renders as a passthrough <see cref="Box"/> with
/// <c>Modifier.FillMaxSize().Padding(...)</c>, walking
/// <see cref="IContentView.PresentedContent"/> through the same
/// <see cref="ComposeWalker"/> pipeline used by
/// <see cref="PageHandler"/>.
/// </summary>
/// <remarks>
/// <para>This handler collides with stock MAUI's
/// <see cref="Microsoft.Maui.Handlers.ContentViewHandler"/>. It wins
/// when registered last in
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>
/// — Compose-backed pages then folds <c>ContentView</c> into the
/// single composition; mixed pages still get the stock platform
/// view.</para>
///
/// <para><see cref="MauiVisualElement.BackgroundColor"/> is mapped
/// onto the wrapping <c>Box</c>'s <c>Modifier.Background(color)</c>
/// rather than going through <see cref="ViewHandler.MapBackground"/>:
/// on the Compose-aware <see cref="IComposeHandler.BuildNode"/> path
/// the handler's own <see cref="ComposeView"/> never gets attached,
/// so the stock background mapper would write a colour to a detached
/// view (the parent renders the wrapping <c>Box</c> directly).</para>
/// </remarks>
public partial class ContentViewHandler : ComposeElementHandler<IContentView>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IContentView"/>
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IContentView, ContentViewHandler> Mapper =
        new PropertyMapper<IContentView, ContentViewHandler>(ViewHandler.ViewMapper)
        {
            ["Content"]                                  = MapContent,
            [nameof(IPadding.Padding)]                   = MapPadding,
            [nameof(MauiVisualElement.BackgroundColor)]  = MapBackgroundColor,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IContentView, ContentViewHandler> CommandMapper =
        new(ViewCommandMapper);

    // Padding/Content can't live in MutableState<T> directly — bump
    // version slots and re-read live (same trick as LayoutHandler).
    readonly MutableState<int>   _paddingVersion  = new(0);
    readonly MutableState<int>   _contentVersion  = new(0);
    readonly MutableState<long?> _backgroundColor = new((long?)null);

    /// <summary>Construct a handler with the default mappers.</summary>
    public ContentViewHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ContentViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        _ = _paddingVersion.Value;
        _ = _contentVersion.Value;

        var view    = VirtualView
            ?? throw new InvalidOperationException("VirtualView not set on ContentViewHandler.");
        var context = MauiContext
            ?? throw new InvalidOperationException("MauiContext not set on ContentViewHandler.");
        var padding = (view as IPadding)?.Padding ?? Thickness.Zero;

        Modifier modifier = Modifier.Companion.FillMaxSize();
        if (_backgroundColor.Value is long bg)
            modifier = modifier.Background(ComposeColor.FromPacked(bg));
        if (padding != Thickness.Zero)
        {
            modifier = modifier.Padding(
                start:  new Dp((float)padding.Left),
                top:    new Dp((float)padding.Top),
                end:    new Dp((float)padding.Right),
                bottom: new Dp((float)padding.Bottom));
        }
        modifier = modifier.ApplyGestures(view, context).ApplySemantics(view);

        var box = new Box { Modifier = modifier };
        if (view.PresentedContent is { } content)
            box.Add(c => ComposeWalker.Render(content, c, context));
        return box;
    }

    /// <summary>Bump the content version slot.</summary>
    public static void MapContent(ContentViewHandler handler, IContentView _) =>
        handler._contentVersion.Value++;

    /// <summary>Bump the padding version slot.</summary>
    public static void MapPadding(ContentViewHandler handler, IContentView _) =>
        handler._paddingVersion.Value++;

    /// <summary>
    /// Map <see cref="MauiVisualElement.BackgroundColor"/> onto the
    /// wrapping <c>Box</c>'s <c>Modifier.Background(...)</c>. Reads
    /// the colour off the concrete <see cref="MauiVisualElement"/>
    /// (the property doesn't surface on <see cref="IView"/>).
    /// </summary>
    public static void MapBackgroundColor(ContentViewHandler handler, IContentView view) =>
        handler._backgroundColor.Value =
            ColorMapping.ToPackedLong((view as MauiVisualElement)?.BackgroundColor);
}
