using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>BottomSheetScaffold</c>. Hosts a persistent bottom
/// sheet alongside a primary content area, plus optional top bar and
/// snackbar slots.
///
/// <see cref="BottomSheetScaffoldKt.RememberBottomSheetScaffoldState"/>
/// is called directly on the C# binding (NOT stripped), threading the
/// composer through.
/// </summary>
public sealed class BottomSheetScaffold : ComposableContainer
{
    /// <summary>Required: the persistent bottom-sheet content.</summary>
    public ComposableNode? SheetContent { get; set; }

    /// <summary>Optional: the sheet's drag handle.</summary>
    public ComposableNode? SheetDragHandle { get; set; }

    /// <summary>Optional: persistent top bar above the main content.</summary>
    public ComposableNode? TopBar { get; set; }

    internal override void Render(IComposer composer)
    {
        if (SheetContent is null)
            throw new System.InvalidOperationException(
                "BottomSheetScaffold.SheetContent is required.");

        // Bound C# call — RememberBottomSheetScaffoldState is NOT stripped.
        var scaffoldState = BottomSheetScaffoldKt.RememberBottomSheetScaffoldState(
            bottomSheetState:   null,
            snackbarHostState:  null,
            _composer:          composer,
            p3:                 0,
            _changed:           3);

        var sheet   = new ComposableLambda3(c => SheetContent.Render(c));
        var content = new ComposableLambda3(c => RenderChildren(c));

        ComposableLambda2? dragHandle = SheetDragHandle is null ? null
            : new ComposableLambda2(c => SheetDragHandle.Render(c));
        ComposableLambda2? topBar = TopBar is null ? null
            : new ComposableLambda2(c => TopBar.Render(c));

        int defaults = (int)BottomSheetScaffoldDefault.All;
        var modifier = BuildModifier();
        if (modifier   is not null) defaults &= ~(int)BottomSheetScaffoldDefault.Modifier;
        if (dragHandle is not null) defaults &= ~(int)BottomSheetScaffoldDefault.SheetDragHandle;
        if (topBar     is not null) defaults &= ~(int)BottomSheetScaffoldDefault.TopBar;

        ComposeBridges.BottomSheetScaffold(
            sheetContent:    sheet,
            modifier:        modifier,
            scaffoldState:   ((Java.Lang.Object)scaffoldState).Handle,
            sheetDragHandle: dragHandle,
            topBar:          topBar,
            snackbarHost:    null,
            content:         content,
            defaults:        defaults,
            composer:        composer);
    }
}
