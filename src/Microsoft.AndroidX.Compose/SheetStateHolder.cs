using AndroidX.Compose.Material3;

namespace AndroidX.Compose;

/// <summary>
/// Caller-supplied state holder for <see cref="ModalBottomSheet"/> or
/// <see cref="BottomSheetScaffold"/>.
/// Wraps Kotlin's <c>SheetState</c> so a facade can carry an
/// initial modal <see cref="SkipPartiallyExpanded"/> setting across
/// recompositions and expose the live sheet position back to C#.
/// </summary>
/// <remarks>
/// <para>Construct one inside <c>Remember</c> so the same instance
/// survives recomposition:</para>
/// <code>
/// var sheet = Remember(() =&gt; new SheetStateHolder(skipPartiallyExpanded: true));
///
/// new ModalBottomSheet(onDismissRequest: () =&gt; show.Value = false, sheetState: sheet)
/// {
///     ConfirmValueChange = v =&gt; !formIsDirty || v != SheetValue.Hidden,
///     new Column { ... },
/// }
/// </code>
/// <para>Imperative open / close from C# uses the
/// <see cref="ShowAsync"/> / <see cref="HideAsync"/> /
/// <see cref="ExpandAsync"/> / <see cref="PartialExpandAsync"/>
/// helpers — the returned <see cref="Task"/> completes when the
/// animation lands.</para>
/// <para>After an owner retires, the next factory preserves compatible
/// settled values. A standard scaffold maps retained <c>Hidden</c> to
/// <c>PartiallyExpanded</c>, making the sheet visible because that factory
/// disallows hiding. A modal owner with <see cref="SkipPartiallyExpanded"/>
/// enabled maps retained <c>PartiallyExpanded</c> to <c>Expanded</c>.
/// These mappings apply only when initializing the receiving factory;
/// unbound current/target values and visibility still describe the last
/// settled state.</para>
/// </remarks>
public sealed class SheetStateHolder
{
    internal SheetState? Jvm;
    SheetValue? _rememberValue;
    SheetValue RetainedValue => _rememberValue ?? SheetValue.Hidden
        ?? throw new InvalidOperationException("SheetValue.Hidden was unavailable.");
    internal SheetValue RememberValue
    {
        get
        {
            var value = RetainedValue;
            return SkipPartiallyExpanded && value == SheetValue.PartiallyExpanded
                ? SheetValue.Expanded ?? throw new InvalidOperationException("SheetValue.Expanded was unavailable.")
                : value;
        }
    }
    internal SheetValue RememberStandardValue
    {
        get
        {
            var value = _rememberValue;
            return value is null || value == SheetValue.Hidden
                ? SheetValue.PartiallyExpanded ?? throw new InvalidOperationException("SheetValue.PartiallyExpanded was unavailable.")
                : value;
        }
    }

    internal void UnbindJvm()
    {
        if (Jvm is not { } jvm)
            return;
        try
        {
            _rememberValue = jvm.CurrentValue;
        }
        finally
        {
            Jvm = null;
        }
    }

    internal void BindJvm(SheetState jvm) => Jvm = jvm;

    /// <summary>
    /// When <c>true</c>, the sheet skips the half-expanded resting
    /// state and either hides fully or expands fully. Mirrors Kotlin
    /// <c>rememberModalBottomSheetState(skipPartiallyExpanded = …)</c>.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool SkipPartiallyExpanded { get; }

    /// <summary>
    /// Construct a holder, optionally opting out of the partially-
    /// expanded resting state.
    /// </summary>
    public SheetStateHolder(bool skipPartiallyExpanded = false)
    {
        SkipPartiallyExpanded = skipPartiallyExpanded;
    }

    /// <summary>
    /// The sheet's current <see cref="SheetValue"/>. While unbound,
    /// returns the last settled value, or <see cref="SheetValue.Hidden"/>
    /// before its first native owner.
    /// </summary>
    public SheetValue CurrentValue => Jvm?.CurrentValue ?? RetainedValue;

    /// <summary>
    /// The sheet's target <see cref="SheetValue"/> during animation,
    /// or the resting state otherwise. While unbound, returns the last
    /// settled value, or <see cref="SheetValue.Hidden"/> before first ownership.
    /// </summary>
    public SheetValue TargetValue => Jvm?.TargetValue ?? RetainedValue;

    /// <summary>
    /// <c>true</c> when the sheet is non-hidden — equivalent to
    /// <c>CurrentValue != SheetValue.Hidden</c>. While unbound, reflects
    /// the retained settled value rather than an active layout.
    /// </summary>
    public bool IsVisible => Jvm?.IsVisible ?? (RetainedValue != SheetValue.Hidden);

    /// <summary>
    /// <c>true</c> when the sheet's anchored set includes a fully-
    /// expanded resting position. Returns <c>false</c> until bound.
    /// </summary>
    public bool HasExpandedState => Jvm?.HasExpandedState ?? false;

    /// <summary>
    /// <c>true</c> when the sheet's anchored set includes a half-
    /// expanded resting position. Returns <c>false</c> until bound,
    /// or when <see cref="SkipPartiallyExpanded"/> is in effect.
    /// </summary>
    public bool HasPartiallyExpandedState => Jvm?.HasPartiallyExpandedState ?? false;

    /// <summary>
    /// Animate the sheet to the half-expanded state (or fully expand
    /// when partial expand is disabled). Mirrors Kotlin
    /// <c>SheetState.show()</c>. Throws
    /// <see cref="InvalidOperationException"/> if invoked before the
    /// holder is bound to a live peer.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels the returned task and stops the underlying Kotlin
    /// animation at its next cancellable suspend point.
    /// </param>
    public Task ShowAsync(CancellationToken cancellationToken = default)
    {
        var jvm = RequireJvm(nameof(ShowAsync));
        return SuspendBridge.Invoke(cont =>
            ComposeBridges.SheetStateShow(((Java.Lang.Object)jvm).Handle, cont),
            cancellationToken);
    }

    /// <summary>
    /// Animate the sheet to <see cref="SheetValue.Hidden"/>. Mirrors
    /// Kotlin <c>SheetState.hide()</c>. See <see cref="ShowAsync"/>
    /// for the binding caveat.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels the returned task and stops the underlying Kotlin
    /// animation at its next cancellable suspend point.
    /// </param>
    public Task HideAsync(CancellationToken cancellationToken = default)
    {
        var jvm = RequireJvm(nameof(HideAsync));
        return SuspendBridge.Invoke(cont =>
            ComposeBridges.SheetStateHide(((Java.Lang.Object)jvm).Handle, cont),
            cancellationToken);
    }

    /// <summary>
    /// Animate the sheet to the fully expanded state. Mirrors
    /// Kotlin <c>SheetState.expand()</c>. See <see cref="ShowAsync"/>
    /// for the binding caveat.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels the returned task and stops the underlying Kotlin
    /// animation at its next cancellable suspend point.
    /// </param>
    public Task ExpandAsync(CancellationToken cancellationToken = default)
    {
        var jvm = RequireJvm(nameof(ExpandAsync));
        return SuspendBridge.Invoke(cont =>
            ComposeBridges.SheetStateExpand(((Java.Lang.Object)jvm).Handle, cont),
            cancellationToken);
    }

    /// <summary>
    /// Animate the sheet to <see cref="SheetValue.PartiallyExpanded"/>.
    /// No-op when the sheet was constructed with
    /// <see cref="SkipPartiallyExpanded"/> = <c>true</c>. Mirrors
    /// Kotlin <c>SheetState.partialExpand()</c>. See
    /// <see cref="ShowAsync"/> for the binding caveat.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancels the returned task and stops the underlying Kotlin
    /// animation at its next cancellable suspend point.
    /// </param>
    public Task PartialExpandAsync(CancellationToken cancellationToken = default)
    {
        var jvm = RequireJvm(nameof(PartialExpandAsync));
        return SuspendBridge.Invoke(cont =>
            ComposeBridges.SheetStatePartialExpand(((Java.Lang.Object)jvm).Handle, cont),
            cancellationToken);
    }

    SheetState RequireJvm(string member) =>
        Jvm ?? throw new InvalidOperationException(
            "SheetStateHolder." + member + " requires a live composition owner; remember or render the holder before controlling it.");
}
