using AndroidX.Compose.Runtime;
using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
    /// <summary>Renders a typed vertical lazy list in the implicit composition.</summary>
    [Composable]
    public static void LazyColumn<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        LazyListState? state = null,
        bool reverseLayout = false,
        PaddingValues? contentPadding = null,
        Arrangement? verticalArrangement = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyColumn<T>(
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ReverseLayout = reverseLayout,
            ContentPadding = contentPadding,
            VerticalArrangement = verticalArrangement,
        }.Render();
    }

    /// <summary>Renders a typed vertical lazy list with an explicit composer.</summary>
    [Composable]
    internal static void LazyColumn<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        LazyListState? state = null,
        bool reverseLayout = false,
        PaddingValues? contentPadding = null,
        Arrangement? verticalArrangement = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyColumn<T>(
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ReverseLayout = reverseLayout,
            ContentPadding = contentPadding,
            VerticalArrangement = verticalArrangement,
        }.Render(composer);
    }

    /// <summary>Renders a typed horizontal lazy list in the implicit composition.</summary>
    [Composable]
    public static void LazyRow<T>(
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        LazyListState? state = null,
        PaddingValues? contentPadding = null,
        Arrangement? horizontalArrangement = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyRow<T>(
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            HorizontalArrangement = horizontalArrangement,
        }.Render();
    }

    /// <summary>Renders a typed horizontal lazy list with an explicit composer.</summary>
    [Composable]
    internal static void LazyRow<T>(
        IComposer composer,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        LazyListState? state = null,
        PaddingValues? contentPadding = null,
        Arrangement? horizontalArrangement = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyRow<T>(
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            HorizontalArrangement = horizontalArrangement,
        }.Render(composer);
    }

    /// <summary>Renders a typed vertically scrolling lazy grid in the implicit composition.</summary>
    [Composable]
    public static void LazyVerticalGrid<T>(
        GridCells columns,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        LazyGridState? state = null,
        PaddingValues? contentPadding = null,
        Arrangement? verticalArrangement = null,
        Arrangement? horizontalArrangement = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyVerticalGrid<T>(
            columns,
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            VerticalArrangement = verticalArrangement,
            HorizontalArrangement = horizontalArrangement,
        }.Render();
    }

    /// <summary>Renders a typed vertically scrolling lazy grid with an explicit composer.</summary>
    [Composable]
    internal static void LazyVerticalGrid<T>(
        IComposer composer,
        GridCells columns,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        LazyGridState? state = null,
        PaddingValues? contentPadding = null,
        Arrangement? verticalArrangement = null,
        Arrangement? horizontalArrangement = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyVerticalGrid<T>(
            columns,
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
            VerticalArrangement = verticalArrangement,
            HorizontalArrangement = horizontalArrangement,
        }.Render(composer);
    }

    /// <summary>Renders a typed horizontally scrolling lazy grid in the implicit composition.</summary>
    [Composable]
    public static void LazyHorizontalGrid<T>(
        GridCells rows,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        LazyGridState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyHorizontalGrid<T>(
            rows,
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render();
    }

    /// <summary>Renders a typed horizontally scrolling lazy grid with an explicit composer.</summary>
    [Composable]
    internal static void LazyHorizontalGrid<T>(
        IComposer composer,
        GridCells rows,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        LazyGridState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyHorizontalGrid<T>(
            rows,
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render(composer);
    }

    /// <summary>Renders a typed vertical lazy staggered grid in the implicit composition.</summary>
    [Composable]
    public static void LazyVerticalStaggeredGrid<T>(
        StaggeredGridCells columns,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        LazyStaggeredGridState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyVerticalStaggeredGrid<T>(
            columns,
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render();
    }

    /// <summary>Renders a typed vertical lazy staggered grid with an explicit composer.</summary>
    [Composable]
    internal static void LazyVerticalStaggeredGrid<T>(
        IComposer composer,
        StaggeredGridCells columns,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        LazyStaggeredGridState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyVerticalStaggeredGrid<T>(
            columns,
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render(composer);
    }

    /// <summary>Renders a typed horizontal lazy staggered grid in the implicit composition.</summary>
    [Composable]
    public static void LazyHorizontalStaggeredGrid<T>(
        StaggeredGridCells rows,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T> itemContent,
        Modifier? modifier = null,
        LazyStaggeredGridState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyHorizontalStaggeredGrid<T>(
            rows,
            items,
            item => new ComposableContentNode(_ => itemContent(item)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render();
    }

    /// <summary>Renders a typed horizontal lazy staggered grid with an explicit composer.</summary>
    [Composable]
    internal static void LazyHorizontalStaggeredGrid<T>(
        IComposer composer,
        StaggeredGridCells rows,
        IReadOnlyList<T> items,
        [ComposableContent] Action<T, IComposer> itemContent,
        Modifier? modifier = null,
        LazyStaggeredGridState? state = null,
        PaddingValues? contentPadding = null)
    {
        ArgumentNullException.ThrowIfNull(composer);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(itemContent);

        new global::AndroidX.Compose.LazyHorizontalStaggeredGrid<T>(
            rows,
            items,
            item => new ComposableContentNode(c => itemContent(item, c)))
        {
            Modifier = modifier,
            State = state,
            ContentPadding = contentPadding,
        }.Render(composer);
    }

    /// <summary>Remembers draggable state in the implicit composition.</summary>
    public static DraggableState RememberDraggableState(
        Action<float> onDelta,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberDraggableState(
            ComposableContext.Current, onDelta, line, file);

    /// <summary>Remembers lazy-list scroll state in the implicit composition.</summary>
    public static LazyListState RememberLazyListState(
        int initialFirstVisibleItemIndex = 0,
        int initialFirstVisibleItemScrollOffset = 0,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberLazyListState(
            ComposableContext.Current,
            initialFirstVisibleItemIndex,
            initialFirstVisibleItemScrollOffset,
            line,
            file);

    /// <summary>Remembers lazy-grid scroll state in the implicit composition.</summary>
    public static LazyGridState RememberLazyGridState(
        int initialFirstVisibleItemIndex = 0,
        int initialFirstVisibleItemScrollOffset = 0,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberLazyGridState(
            ComposableContext.Current,
            initialFirstVisibleItemIndex,
            initialFirstVisibleItemScrollOffset,
            line,
            file);

    /// <summary>Remembers lazy staggered-grid scroll state in the implicit composition.</summary>
    public static LazyStaggeredGridState RememberLazyStaggeredGridState(
        int initialFirstVisibleItemIndex = 0,
        int initialFirstVisibleItemScrollOffset = 0,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        ComposeExtensions.RememberLazyStaggeredGridState(
            ComposableContext.Current,
            initialFirstVisibleItemIndex,
            initialFirstVisibleItemScrollOffset,
            line,
            file);
}
