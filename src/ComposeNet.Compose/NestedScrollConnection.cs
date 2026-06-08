using Android.Runtime;

namespace ComposeNet;

/// <summary>
/// Opaque C# wrapper around
/// <c>androidx.compose.ui.input.nestedscroll.NestedScrollConnection</c>.
/// The Kotlin interface defines the four <c>onPreScroll</c> /
/// <c>onPostScroll</c> / <c>onPreFling</c> / <c>onPostFling</c>
/// callbacks Compose uses to negotiate scroll deltas between a
/// nested-scrolling parent (e.g. a Material 3 <c>TopAppBar</c> that
/// collapses on scroll) and its scrollable child.
/// </summary>
/// <remarks>
/// <para>
/// In v1, this wrapper is opaque from C#: callers obtain an instance
/// from <see cref="TopAppBarScrollBehavior.NestedScrollConnection"/>
/// and hand it back to <see cref="Modifier.NestedScroll(NestedScrollConnection)"/>
/// — there is no managed-callback path yet. Implementing a custom
/// connection from C# would require a Kotlin-side adapter (similar to
/// <c>DrawerValueConfirmStateChange</c>) and is tracked separately.
/// </para>
/// <para>
/// The underlying JNI handle is held via the <see cref="Java.Lang.Object"/>
/// base, so the bridge generator's reference-type lowering can pass
/// it straight to <c>L</c> JNI slots without unwrapping.
/// </para>
/// </remarks>
public sealed class NestedScrollConnection : Java.Lang.Object
{
    internal NestedScrollConnection(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }
}
