using System.Runtime.CompilerServices;
using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Static factories mirroring Kotlin's
/// <c>androidx.compose.material3.TopAppBarDefaults</c> singleton —
/// each returns a <see cref="TopAppBarScrollBehavior"/> configured
/// for a particular collapse strategy.
/// </summary>
/// <remarks>
/// <para>
/// All factories are <c>@Composable</c> on the Kotlin side and must
/// be invoked inside a composition (typically inside a
/// <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>
/// lambda or directly inside a <see cref="ComposableNode.Render(IComposer)"/>
/// override). Each call grabs the active composer from
/// <see cref="ComposeContext"/> and wraps the bridge call in a
/// stable replaceable group so the underlying scroll behavior keeps
/// its identity across recompositions.
/// </para>
/// <para>
/// Canonical usage:
/// <code>
/// var state = Compose.Remember(() =&gt; new TopAppBarState());
/// var behavior = TopAppBarDefaults.PinnedScrollBehavior(state);
/// </code>
/// </para>
/// </remarks>
public static class TopAppBarDefaults
{
    /// <summary>
    /// Mirrors Kotlin's
    /// <c>TopAppBarDefaults.pinnedScrollBehavior(state)</c>. The bar
    /// stays in place but tracks the content's scroll offset on
    /// <paramref name="state"/> so it can raise its surface
    /// elevation when content scrolls under it.
    /// </summary>
    /// <param name="state">
    /// Caller-owned <see cref="TopAppBarState"/>; typically obtained
    /// via <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>.
    /// </param>
    public static TopAppBarScrollBehavior PinnedScrollBehavior(
        TopAppBarState state,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        Invoke(state, line, file, ComposeBridges.TopAppBarDefaultsPinnedScrollBehavior);

    /// <summary>
    /// Mirrors Kotlin's
    /// <c>TopAppBarDefaults.enterAlwaysScrollBehavior(state)</c>.
    /// The bar collapses as content scrolls up and immediately
    /// expands again the moment the user scrolls back down, even
    /// before reaching the top.
    /// </summary>
    /// <param name="state">
    /// Caller-owned <see cref="TopAppBarState"/>; typically obtained
    /// via <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>.
    /// </param>
    public static TopAppBarScrollBehavior EnterAlwaysScrollBehavior(
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
    /// <see cref="LargeTopAppBar"/> for the "tall bar that snaps to
    /// a compact bar" pattern.
    /// </summary>
    /// <param name="state">
    /// Caller-owned <see cref="TopAppBarState"/>; typically obtained
    /// via <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/>.
    /// </param>
    public static TopAppBarScrollBehavior ExitUntilCollapsedScrollBehavior(
        TopAppBarState state,
        [CallerLineNumber] int line = 0,
        [CallerFilePath] string file = "") =>
        Invoke(state, line, file, ComposeBridges.TopAppBarDefaultsExitUntilCollapsedScrollBehavior);

    static TopAppBarScrollBehavior Invoke(
        TopAppBarState state, int line, string file,
        System.Func<IntPtr, IComposer, IntPtr> bridge)
    {
        System.ArgumentNullException.ThrowIfNull(state);
        var composer = ComposeContext.Current
            ?? throw new System.InvalidOperationException(
                "TopAppBarDefaults factories must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

        composer.StartReplaceableGroup(SourceLocationKey.Compute(line, file));
        try
        {
            IntPtr jvmStateHandle = ((Java.Lang.Object)state.Jvm).Handle;
            IntPtr handle = bridge(jvmStateHandle, composer);
            try
            {
                return new TopAppBarScrollBehavior(handle, JniHandleOwnership.TransferLocalRef);
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
            System.GC.KeepAlive(state);
            composer.EndReplaceableGroup();
        }
    }
}
