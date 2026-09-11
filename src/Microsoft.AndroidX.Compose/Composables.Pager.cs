using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a positional horizontal pager using the original binary-compatible signature.</summary>
    [Composable]
    public static void HorizontalPager<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier,
        PagerState? state,
        PaddingValues? contentPadding) =>
        HorizontalPager(items, itemContent, modifier, state, contentPadding, key: null);

    /// <summary>Renders a typed horizontal pager in the implicit composition.</summary>
    /// <param name="key">Optional stable string/int/long identity; see <see cref="HorizontalPager{T}.Key"/>.</param>
    [Composable]
    public static void HorizontalPager<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null,
        Func<T, object>? key = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalPager<T>(
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            Key = key,
        }.Render();
    }

    /// <summary>Renders a typed horizontal pager with an explicit composer.</summary>
    /// <param name="key">Optional stable string/int/long identity; see <see cref="HorizontalPager{T}.Key"/>.</param>
    [Composable]
    internal static void HorizontalPager<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null,
        Func<T, object>? key = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.HorizontalPager<T>(
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            Key = key,
        }.Render(composer);
    }

    /// <summary>Renders a positional vertical pager using the original binary-compatible signature.</summary>
    [Composable]
    public static void VerticalPager<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier,
        PagerState? state,
        PaddingValues? contentPadding) =>
        VerticalPager(items, itemContent, modifier, state, contentPadding, key: null);

    /// <summary>Renders a typed vertical pager in the implicit composition.</summary>
    /// <param name="key">Optional stable string/int/long identity; see <see cref="VerticalPager{T}.Key"/>.</param>
    [Composable]
    public static void VerticalPager<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null,
        Func<T, object>? key = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.VerticalPager<T>(
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            Key = key,
        }.Render();
    }

    /// <summary>Renders a typed vertical pager with an explicit composer.</summary>
    /// <param name="key">Optional stable string/int/long identity; see <see cref="VerticalPager{T}.Key"/>.</param>
    [Composable]
    internal static void VerticalPager<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        PagerState? state = null,
        PaddingValues? contentPadding = null,
        Func<T, object>? key = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.VerticalPager<T>(
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            Key = key,
        }.Render(composer);
    }
}
