using System.Runtime.CompilerServices;
using Android.Runtime;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Static factories mirroring Kotlin's
/// <c>androidx.compose.material3.TopAppBarDefaults</c> singleton — each
/// returns an <see cref="ITopAppBarScrollBehavior"/> configured for a
/// particular collapse strategy.
/// </summary>
/// <remarks>
/// <para>
/// All factories are <c>@Composable</c> on the Kotlin side and must be
/// invoked inside a composition (typically inside a
/// <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>
/// lambda or directly inside a <see cref="ComposableNode.Render(IComposer)"/>
/// override). Each call grabs the active composer from
/// <see cref="ComposeContext"/> and wraps the bridge call in a stable
/// replaceable group so the underlying scroll behavior keeps its
/// identity across recompositions.
/// </para>
/// <para>
/// Both the state and the returned behavior are the bound binding
/// types directly — there's no ComposeNet wrapper because the
/// Material3 binding ships <see cref="TopAppBarState"/> (with its
/// <c>(float, float, float)</c> ctor and r/w offset properties) and
/// <see cref="ITopAppBarScrollBehavior"/> (with its
/// <c>NestedScrollConnection</c>, <c>IsPinned</c>, and <c>State</c>
/// members) unmangled. <c>PinnedScrollBehavior</c> calls the binding
/// directly; <c>EnterAlwaysScrollBehavior</c> and
/// <c>ExitUntilCollapsedScrollBehavior</c> go through a
/// <see cref="ComposeBridges"/> JNI helper because the binder strips
/// their <c>AnimationSpec</c> / <c>DecayAnimationSpec</c> parameters.
/// </para>
/// <para>
/// Canonical usage:
/// <code>
/// var state = Compose.Remember(() =&gt;
///     new TopAppBarState(float.NegativeInfinity, 0f, 0f));
/// var behavior = TopAppBarDefaults.PinnedScrollBehavior(state);
/// </code>
/// </para>
/// </remarks>
public static class TopAppBarDefaults
{
    /// <summary>
    /// Mirrors Kotlin's <c>rememberTopAppBarState(...)</c> — allocates
    /// a <see cref="TopAppBarState"/> via
    /// <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>
    /// so the same instance survives recompositions, with the three
    /// initial offsets surfaced as named C# parameters with Kotlin's
    /// defaults.
    /// </summary>
    /// <param name="initialHeightOffsetLimit">
    /// Initial value of <see cref="TopAppBarState.HeightOffsetLimit"/>
    /// — the minimum (most-negative) height offset the bar can collapse
    /// to. Kotlin's default is <see cref="float.NegativeInfinity"/>,
    /// meaning "let the bar measure its own collapsed height when it
    /// first composes."
    /// </param>
    /// <param name="initialHeightOffset">
    /// Initial value of <see cref="TopAppBarState.HeightOffset"/> — the
    /// current offset, between <paramref name="initialHeightOffsetLimit"/>
    /// (fully collapsed) and <c>0</c> (fully expanded).
    /// </param>
    /// <param name="initialContentOffset">
    /// Initial value of <see cref="TopAppBarState.ContentOffset"/> —
    /// the cumulative scroll-content offset used to drive the pinned
    /// bar's elevation tonal overlay.
    /// </param>
    public static TopAppBarState RememberTopAppBarState(
        float initialHeightOffsetLimit = float.NegativeInfinity,
        float initialHeightOffset = 0f,
        float initialContentOffset = 0f,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        Compose.Remember(
            () => new TopAppBarState(
                initialHeightOffsetLimit, initialHeightOffset, initialContentOffset),
            line, file);

    /// <summary>
    /// Mirrors Kotlin's
    /// <c>TopAppBarDefaults.pinnedScrollBehavior(state)</c>. The bar
    /// stays in place but tracks the content's scroll offset on
    /// <paramref name="state"/> so it can raise its surface elevation
    /// when content scrolls under it.
    /// </summary>
    /// <param name="state">
    /// Caller-owned <see cref="TopAppBarState"/>; typically obtained
    /// via <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>.
    /// </param>
    public static ITopAppBarScrollBehavior PinnedScrollBehavior(
        TopAppBarState state,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "")
    {
        ArgumentNullException.ThrowIfNull(state);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "TopAppBarDefaults factories must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            // canScroll: null → Kotlin's `() -> true` default; bit 1 in
            // `$default` flags that we're omitting it.
            return AndroidX.Compose.Material3.TopAppBarDefaults.Instance
                .PinnedScrollBehavior(state, canScroll: null, composer, 0, 0b10);
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }

    /// <summary>
    /// Mirrors Kotlin's
    /// <c>TopAppBarDefaults.enterAlwaysScrollBehavior(state)</c>. The
    /// bar collapses as content scrolls up and immediately expands
    /// again the moment the user scrolls back down, even before
    /// reaching the top.
    /// </summary>
    /// <param name="state">
    /// Caller-owned <see cref="TopAppBarState"/>; typically obtained
    /// via <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>.
    /// </param>
    public static ITopAppBarScrollBehavior EnterAlwaysScrollBehavior(
        TopAppBarState state,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        Invoke(state, line, file, ComposeBridges.TopAppBarDefaultsEnterAlwaysScrollBehavior);

    /// <summary>
    /// Mirrors Kotlin's
    /// <c>TopAppBarDefaults.exitUntilCollapsedScrollBehavior(state)</c>.
    /// The bar collapses as content scrolls up but only re-expands
    /// once the content scrolls back to the very top — typically
    /// paired with <see cref="MediumTopAppBar"/> or
    /// <see cref="LargeTopAppBar"/> for the "tall bar that snaps to a
    /// compact bar" pattern.
    /// </summary>
    /// <param name="state">
    /// Caller-owned <see cref="TopAppBarState"/>; typically obtained
    /// via <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>.
    /// </param>
    public static ITopAppBarScrollBehavior ExitUntilCollapsedScrollBehavior(
        TopAppBarState state,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        Invoke(state, line, file, ComposeBridges.TopAppBarDefaultsExitUntilCollapsedScrollBehavior);

    static ITopAppBarScrollBehavior Invoke(
        TopAppBarState state, int line, string file,
        System.Func<TopAppBarState, IComposer, IntPtr> bridge)
    {
        ArgumentNullException.ThrowIfNull(state);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "TopAppBarDefaults factories must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            IntPtr handle = bridge(state, composer);
            try
            {
                return Java.Lang.Object.GetObject<ITopAppBarScrollBehavior>(
                    handle, JniHandleOwnership.TransferLocalRef)!;
            }
            catch
            {
                if (handle != IntPtr.Zero)
                    JNIEnv.DeleteLocalRef(handle);
                throw;
            }
        }
        finally
        {
            composer.EndReplaceableGroup();
        }
    }
}
