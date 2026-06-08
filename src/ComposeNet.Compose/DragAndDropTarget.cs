using Android.Runtime;
using AndroidX.Compose.UI.Draganddrop;

namespace ComposeNet;

/// <summary>
/// <c>androidx.compose.ui.draganddrop.DragAndDropTarget</c> adapter — the
/// receiver of drag-and-drop callbacks installed by
/// <see cref="Modifier.DragAndDropTarget(System.Func{ComposeNet.DragAndDropEvent, bool}, ComposeNet.DragAndDropTarget)"/>.
/// Set <see cref="OnDrop"/> to a delegate that processes the dropped payload
/// and returns <c>true</c> when the drop was consumed.
///
/// The Java peer is created once per managed instance, so
/// <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/> the target
/// to keep the modifier-element identity stable across recompositions —
/// otherwise Compose rebuilds the underlying <c>DragAndDropTargetElement</c>
/// every frame and any internal hover/started/ended bookkeeping resets.
/// <see cref="OnDrop"/> may be reassigned at any time; the new delegate is
/// observed on the next drop.
///
/// The other Kotlin callbacks (<c>onStarted</c>, <c>onEntered</c>,
/// <c>onMoved</c>, <c>onChanged</c>, <c>onExited</c>, <c>onEnded</c>) are
/// no-ops in v1 — Compose's interface defaults are inlined here so callers
/// only need to wire <c>onDrop</c>. Exposing more handlers can land in a
/// follow-up without breaking this surface.
/// </summary>
[Register("composenet/compose/DragAndDropTarget")]
public sealed class DragAndDropTarget : Java.Lang.Object, IDragAndDropTarget
{
    /// <summary>
    /// Delegate invoked when a drag is dropped onto the modified composable.
    /// Return <c>true</c> to mark the drop as consumed (matches Kotlin
    /// <c>onDrop</c>'s contract). When <c>null</c> the target reports the
    /// drop as unhandled and the system propagates it to the next eligible
    /// listener.
    /// </summary>
    public System.Func<DragAndDropEvent, bool>? OnDrop { get; set; }

    /// <summary>Construct an unwired target. Assign <see cref="OnDrop"/> before installing it on a modifier chain.</summary>
    public DragAndDropTarget() { }

    /// <summary>Convenience ctor that wires <paramref name="onDrop"/> in one expression.</summary>
    public DragAndDropTarget(System.Func<DragAndDropEvent, bool> onDrop)
    {
        OnDrop = onDrop;
    }

    bool IDragAndDropTarget.OnDrop(AndroidX.Compose.UI.Draganddrop.DragAndDropEvent p0)
    {
        var cb = OnDrop;
        if (cb is null)
            return false;
        return cb(new DragAndDropEvent(p0));
    }

    void IDragAndDropTarget.OnChanged(AndroidX.Compose.UI.Draganddrop.DragAndDropEvent p0) { }
    void IDragAndDropTarget.OnEnded(AndroidX.Compose.UI.Draganddrop.DragAndDropEvent p0) { }
    void IDragAndDropTarget.OnEntered(AndroidX.Compose.UI.Draganddrop.DragAndDropEvent p0) { }
    void IDragAndDropTarget.OnExited(AndroidX.Compose.UI.Draganddrop.DragAndDropEvent p0) { }
    void IDragAndDropTarget.OnMoved(AndroidX.Compose.UI.Draganddrop.DragAndDropEvent p0) { }
    void IDragAndDropTarget.OnStarted(AndroidX.Compose.UI.Draganddrop.DragAndDropEvent p0) { }
}
