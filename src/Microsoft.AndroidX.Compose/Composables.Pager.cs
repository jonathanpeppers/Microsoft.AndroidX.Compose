using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a typed horizontal pager in the implicit composition.</summary>
    [Composable]
    public static void HorizontalPager<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalPager<T>(
            items,
            item => new Tier2InlineContent(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render(ComposableContext.Current);
    }

    /// <summary>Renders a typed horizontal pager with an explicit composer.</summary>
    [Composable]
    public static void HorizontalPager<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalPager<T>(
            items,
            item => new Tier2InlineContent(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render(composer);
    }

    /// <summary>Renders a typed vertical pager in the implicit composition.</summary>
    [Composable]
    public static void VerticalPager<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.VerticalPager<T>(
            items,
            item => new Tier2InlineContent(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render(ComposableContext.Current);
    }

    /// <summary>Renders a typed vertical pager with an explicit composer.</summary>
    [Composable]
    public static void VerticalPager<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.VerticalPager<T>(
            items,
            item => new Tier2InlineContent(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render(composer);
    }
}
