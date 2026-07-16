using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 <c>BottomSheetScaffold</c>. Hosts a persistent bottom
/// sheet alongside a primary content area, plus optional top bar and
/// snackbar slots. Pass an optional <see cref="SheetStateHolder"/> to
/// configure the underlying <c>SheetState</c>; set
/// <see cref="ConfirmValueChange"/> to veto user-initiated transitions.
/// </summary>
/// <remarks>
/// Stays hand-written: the Kotlin signature has two non-nullable
/// <c>@Composable</c> lambdas (<c>sheetContent</c> and <c>content</c>),
/// which the source generator's hybrid-container path doesn't yet
/// support. The new <see cref="SheetStateHolder"/> /
/// <see cref="ConfirmValueChange"/> wiring mirrors the generator's
/// <c>[StateHolder] + [ConfirmStateChange]</c> contract so a future
/// migration is mechanical.
///
/// <code>
/// var sheet = Remember(() =&gt; new SheetStateHolder(skipPartiallyExpanded: false));
/// new BottomSheetScaffold(sheetState: sheet)
/// {
///     ConfirmValueChange = v =&gt; v != SheetValue.Hidden,
///     SheetContent = new Column { new Text("Sheet") },
///     TopBar       = new TopAppBar { Title = new Text("App") },
///     new Text("Main content"),
/// }
/// </code>
/// </remarks>
public sealed class BottomSheetScaffold : ComposableContainer
{
    readonly SheetStateHolder? _sheetState;

    // One JCW per scaffold instance — its JNI identity is part of the
    // Kotlin `remember` cache key, so allocating fresh each render
    // would invalidate the cached state holder. Callback is read on
    // every Invoke, so callers can mutate ConfirmValueChange freely.
    readonly SheetValueConfirmStateChange _confirmValueChangeAdapter = new();

    /// <summary>
    /// Construct the scaffold. Pass a <paramref name="sheetState"/>
    /// to opt into the <c>skipPartiallyExpanded</c> setting and gain
    /// imperative control via <see cref="SheetStateHolder.ShowAsync"/>
    /// / <see cref="SheetStateHolder.HideAsync"/>.
    /// </summary>
    public BottomSheetScaffold(SheetStateHolder? sheetState = null)
    {
        _sheetState = sheetState;
    }

    /// <summary>Required: the persistent bottom-sheet content.</summary>
    public ComposableNode? SheetContent { get; set; }

    internal ComposableNode? Tier2Content { get; set; }

    /// <summary>Optional: the sheet's drag handle.</summary>
    public ComposableNode? SheetDragHandle { get; set; }

    /// <summary>Optional: persistent top bar above the main content.</summary>
    public ComposableNode? TopBar { get; set; }

    /// <summary>
    /// Veto callback consulted on every sheet transition. Return
    /// <c>false</c> to refuse the transition (gesture or programmatic).
    /// <c>null</c> behaves as Kotlin's default <c>{ true }</c> —
    /// every transition allowed.
    /// </summary>
    public Func<SheetValue, bool>? ConfirmValueChange { get; set; }

    public override void Render(IComposer composer)
    {
        if (SheetContent is null)
            throw new InvalidOperationException(
                "BottomSheetScaffold.SheetContent is required.");

        // Update the JCW callback BEFORE the Remember calls. Compose
        // captures the IFunction1's JNI peer at first composition; we
        // keep the peer's identity stable but read Callback fresh on
        // every Invoke so the user can mutate ConfirmValueChange.
        _confirmValueChangeAdapter.Callback = ConfirmValueChange;

        // Keep the remember-call shape constant even when
        // ConfirmValueChange toggles between null and non-null. The stable
        // adapter treats null as "allow", matching Kotlin's default.
        var sheetState = BottomSheetScaffoldKt.RememberStandardBottomSheetState(
            initialValue:        SheetValue.PartiallyExpanded,
            confirmValueChange:  _confirmValueChangeAdapter,
            skipHiddenState:     true,
            _composer:           composer,
            p4:                  0,
            _changed:            0);
        if (_sheetState is not null)
            _sheetState.Jvm = sheetState;

        // Bound C# call — RememberBottomSheetScaffoldState is NOT stripped.
        // _changed (= Kotlin's $default) bit 0 = bottomSheetState defaulted,
        // bit 1 = snackbarHostState defaulted. We always supply the stable
        // bottom-sheet state and never wire snackbar, so only bit 1 stays set.
        const int changed = 2;
        var scaffoldState = BottomSheetScaffoldKt.RememberBottomSheetScaffoldState(
            bottomSheetState:   sheetState,
            snackbarHostState:  null,
            _composer:          composer,
            p3:                 0,
            _changed:           changed);

        var sheet = ComposableLambdas.Wrap3(
            composer,
            c => SheetContent.Render(c));
        var tier2Content = Tier2Content;
        var content = ComposableLambdas.Wrap3(
            composer,
            c =>
            {
                if (tier2Content is null)
                    RenderChildren(c);
                else
                    tier2Content.Render(c);
            });

        var dragHandle = SheetDragHandle is null ? null
            : ComposableLambdas.Wrap2(composer, c => SheetDragHandle.Render(c));
        var topBar = TopBar is null ? null
            : ComposableLambdas.Wrap2(composer, c => TopBar.Render(c));

        int defaults = (int)BottomSheetScaffoldDefault.All;
        var __modifierKey = BuildModifierStructuralKey();
        var modifier = BuildModifier();
        if (modifier   is not null) defaults &= ~(int)BottomSheetScaffoldDefault.Modifier;
        if (dragHandle is not null) defaults &= ~(int)BottomSheetScaffoldDefault.SheetDragHandle;
        if (topBar     is not null) defaults &= ~(int)BottomSheetScaffoldDefault.TopBar;

        // $changed mask: param 0=sheetContent (Wrap3 → Static),
        // 1=modifier (DiffSlot key), 2=scaffoldState (Jvm reference
        // DiffSlot — same instance across recompositions when state
        // holder is cached), 3=sheetDragHandle (Function2? identity),
        // 4=topBar (Function2? identity), 5=snackbarHost (null → Same),
        // 6=content (Wrap3 → Static).
        int __changed = 0;
        __changed |= (int)ChangedBits.Static << ComposeExtensions.DiffSlotShift(0);
        __changed |= composer.DiffSlot(__modifierKey, ComposeExtensions.DiffSlotShift(1));
        __changed |= composer.DiffSlot(scaffoldState, ComposeExtensions.DiffSlotShift(2));
        __changed |= composer.DiffSlot<object?>(dragHandle, ComposeExtensions.DiffSlotShift(3));
        __changed |= composer.DiffSlot<object?>(topBar, ComposeExtensions.DiffSlotShift(4));
        __changed |= (int)ChangedBits.Same << ComposeExtensions.DiffSlotShift(5);
        __changed |= (int)ChangedBits.Static << ComposeExtensions.DiffSlotShift(6);

        ComposeBridges.BottomSheetScaffold(
            sheetContent:    sheet,
            modifier:        modifier,
            scaffoldState:   ((Java.Lang.Object)scaffoldState).Handle,
            sheetDragHandle: dragHandle,
            topBar:          topBar,
            snackbarHost:    null,
            content:         content,
            defaults:        defaults,
            composer:        composer,
            _changed:        __changed);
    }
}
