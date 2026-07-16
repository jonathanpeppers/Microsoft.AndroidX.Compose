using AndroidX.Compose.Material3.Carousel;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a typed uncontained carousel in the implicit composition.</summary>
    [Composable]
    public static void HorizontalUncontainedCarousel<T>(
        IReadOnlyList<T> items,
        float itemWidth,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        CarouselState? state = null,
        Dp? itemSpacing = null,
        bool? userScrollEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalUncontainedCarousel<T>(
            items,
            itemWidth,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ItemSpacing = itemSpacing,
            UserScrollEnabled = userScrollEnabled,
        }.Render();
    }

    /// <summary>Renders a typed uncontained carousel with an explicit composer.</summary>
    [Composable]
    internal static void HorizontalUncontainedCarousel<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        float itemWidth,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        CarouselState? state = null,
        Dp? itemSpacing = null,
        bool? userScrollEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalUncontainedCarousel<T>(
            items,
            itemWidth,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ItemSpacing = itemSpacing,
            UserScrollEnabled = userScrollEnabled,
        }.Render(composer);
    }

    /// <summary>Renders a typed multi-browse carousel in the implicit composition.</summary>
    [Composable]
    public static void HorizontalMultiBrowseCarousel<T>(
        IReadOnlyList<T> items,
        float preferredItemWidth,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        CarouselState? state = null,
        Dp? itemSpacing = null,
        bool? userScrollEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalMultiBrowseCarousel<T>(
            items,
            preferredItemWidth,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ItemSpacing = itemSpacing,
            UserScrollEnabled = userScrollEnabled,
        }.Render();
    }

    /// <summary>Renders a typed multi-browse carousel with an explicit composer.</summary>
    [Composable]
    internal static void HorizontalMultiBrowseCarousel<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        float preferredItemWidth,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        CarouselState? state = null,
        Dp? itemSpacing = null,
        bool? userScrollEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalMultiBrowseCarousel<T>(
            items,
            preferredItemWidth,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ItemSpacing = itemSpacing,
            UserScrollEnabled = userScrollEnabled,
        }.Render(composer);
    }

    /// <summary>Renders a typed centered-hero carousel in the implicit composition.</summary>
    [Composable]
    public static void HorizontalCenteredHeroCarousel<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        CarouselState? state = null,
        Dp? maxItemWidth = null,
        Dp? itemSpacing = null,
        bool? userScrollEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalCenteredHeroCarousel<T>(
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            MaxItemWidth = maxItemWidth,
            ItemSpacing = itemSpacing,
            UserScrollEnabled = userScrollEnabled,
        }.Render();
    }

    /// <summary>Renders a typed centered-hero carousel with an explicit composer.</summary>
    [Composable]
    internal static void HorizontalCenteredHeroCarousel<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        CarouselState? state = null,
        Dp? maxItemWidth = null,
        Dp? itemSpacing = null,
        bool? userScrollEnabled = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalCenteredHeroCarousel<T>(
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            MaxItemWidth = maxItemWidth,
            ItemSpacing = itemSpacing,
            UserScrollEnabled = userScrollEnabled,
        }.Render(composer);
    }
}
