namespace AndroidX.Compose;

internal sealed record LongPressDragCallbacks(
    Action<Offset> OnDrag,
    Action<Offset>? OnDragStart,
    Action? OnDragEnd,
    Action? OnDragCancel);
