using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// C# wrapper around
/// <c>androidx.compose.material3.TopAppBarScrollBehavior</c> — the
/// strategy object returned by the factories on
/// <see cref="TopAppBarDefaults"/>. Pass an instance to one of the
/// Material 3 top app bar facades via their <c>ScrollBehavior</c>
/// property AND wire its <see cref="NestedScrollConnection"/> into
/// the scrolling container via
/// <see cref="Modifier.NestedScroll(NestedScrollConnection)"/> so
/// both sides agree on how scroll deltas drive the bar's collapse.
/// </summary>
/// <remarks>
/// <para>
/// The Material3 binding ships the underlying Kotlin interface
/// (<c>ITopAppBarScrollBehavior</c>) with <c>IsPinned</c> and
/// <c>State</c> getters bound, but strips the
/// <c>getNestedScrollConnection()</c> accessor because its return
/// type lives in <c>ui-android-nestedscroll</c> (which isn't bound).
/// This wrapper restores that accessor via
/// <see cref="ComposeBridges.GetNestedScrollConnection(System.IntPtr)"/>.
/// </para>
/// <para>
/// Instances are constructed by the factory methods on
/// <see cref="TopAppBarDefaults"/>
/// (<c>PinnedScrollBehavior</c>, <c>EnterAlwaysScrollBehavior</c>,
/// <c>ExitUntilCollapsedScrollBehavior</c>) — there is no public
/// constructor. The wrapper derives from
/// <see cref="Java.Lang.Object"/> so the bridge generator can pass
/// its <c>.Handle</c> straight to the <c>L</c> JNI slot in the
/// underlying <c>TopAppBar</c> bridge.
/// </para>
/// </remarks>
public sealed class TopAppBarScrollBehavior : Java.Lang.Object
{
    NestedScrollConnection? _connection;

    internal TopAppBarScrollBehavior(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    /// <summary>
    /// The <see cref="ComposeNet.NestedScrollConnection"/> this
    /// behavior expects to receive scroll deltas through. Wire it to
    /// your scrolling container via
    /// <see cref="Modifier.NestedScroll(NestedScrollConnection)"/>;
    /// without this connection the bar will render but never
    /// collapse / expand in response to scrolling.
    /// </summary>
    /// <remarks>
    /// The handle is resolved lazily on first access and cached for
    /// the lifetime of this wrapper.
    /// </remarks>
    public NestedScrollConnection NestedScrollConnection
    {
        get
        {
            if (_connection is null)
            {
                IntPtr handle = ComposeBridges.GetNestedScrollConnection(Handle);
                _connection = new NestedScrollConnection(handle, JniHandleOwnership.TransferLocalRef);
            }
            return _connection;
        }
    }
}
