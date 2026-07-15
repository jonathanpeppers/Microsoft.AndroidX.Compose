using System.Runtime.CompilerServices;

namespace AndroidX.Compose;

public static partial class Composables
{
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
}
