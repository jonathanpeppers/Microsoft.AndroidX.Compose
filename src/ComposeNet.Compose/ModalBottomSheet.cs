using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>ModalBottomSheet</c>. Opens a modal sheet anchored to
/// the bottom of the screen.
///
/// The <c>SheetState</c> is created inside <see cref="Render"/> via
/// <see cref="ModalBottomSheetKt.RememberModalBottomSheetState"/> — that
/// builder is NOT stripped from the binding, so we call it directly on
/// the bound C# method instead of going through JNI. Use the visibility
/// pattern from <see cref="AlertDialog"/>: gate the entire instance on
/// a <see cref="MutableState{T}"/> of <see cref="bool"/>.
///
/// <code>
/// var show = Remember(() => new MutableState&lt;bool&gt;(false));
/// show.Value
///     ? new ModalBottomSheet(onDismissRequest: () =&gt; show.Value = false)
///       {
///           new Column { new Text("Sheet contents") },
///       }
///     : null
/// </code>
/// </summary>
public sealed class ModalBottomSheet : ComposableContainer
{
    readonly System.Action _onDismissRequest;
    public ModalBottomSheet(System.Action onDismissRequest) => _onDismissRequest = onDismissRequest;

    /// <summary>Optional drag handle drawn at the top of the sheet.</summary>
    public ComposableNode? DragHandle { get; set; }

    internal override void Render(IComposer composer)
    {
        // Bound C# call — RememberModalBottomSheetState is NOT stripped.
        // p3 is the (renamed) Composer parameter; _changed = 3 means bits
        // 0 and 1 (skipPartiallyExpanded, confirmValueChange) are
        // defaulted by Compose.
        var sheetState = ModalBottomSheetKt.RememberModalBottomSheetState(
            skipPartiallyExpanded: false,
            confirmValueChange:    null,
            _composer:             composer,
            p3:                    0,
            _changed:              3);

        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var content   = new ComposableLambda3(c => RenderChildren(c));
        ComposableLambda2? dragHandle = DragHandle is null ? null
            : new ComposableLambda2(c => DragHandle.Render(c));

        int defaults = (int)ModalBottomSheetDefault.All;
        var modifier = BuildModifier();
        if (modifier   is not null) defaults &= ~(int)ModalBottomSheetDefault.Modifier;
        if (dragHandle is not null) defaults &= ~(int)ModalBottomSheetDefault.DragHandle;

        ComposeBridges.ModalBottomSheet(
            onDismissRequest: onDismiss,
            modifier:         modifier,
            sheetState:       ((Java.Lang.Object)sheetState).Handle,
            dragHandle:       dragHandle,
            content:          content,
            defaults:         defaults,
            composer:         composer);
    }
}
