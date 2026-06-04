using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>SnackbarHost</c>. Anchors a <see cref="Snackbar"/>
/// driven by a <see cref="ComposeNet.SnackbarHostState"/>. Typically
/// dropped into <see cref="Scaffold.SnackbarHost"/>:
/// <code>
/// var hostState = Remember(() =&gt; new SnackbarHostState());
///
/// new Scaffold
/// {
///     SnackbarHost = new SnackbarHost(hostState),
///     Body         = ...,
/// }
/// </code>
/// </summary>
/// <remarks>
/// Triggering snackbars via <see cref="ComposeNet.SnackbarHostState"/>
/// requires Kotlin coroutines and isn't yet wired up in this binding.
/// For most cases, render a <see cref="Snackbar"/> directly into the
/// <see cref="Scaffold.SnackbarHost"/> slot, gated by a
/// <see cref="MutableState{T}"/>.
/// </remarks>
public sealed class SnackbarHost : ComposableNode
{
    readonly SnackbarHostState _hostState;

    public SnackbarHost(SnackbarHostState hostState) => _hostState = hostState;

    internal override void Render(IComposer composer)
    {
        // SnackbarHost's Function3 receives the current SnackbarData as p0.
        // We forward to the default Snackbar renderer by calling the
        // default-shaped Snackbar(snackbarData) Kotlin overload — but
        // since that's a stripped variant we just emit a plain Snackbar
        // with no message when invoked directly. In practice the host
        // is rarely visited because the suspending trigger isn't bridged.
        var snackbar = new ComposableLambda3((_, c) =>
        {
            new Snackbar { Body = new Text(string.Empty) }.Render(c);
        });

        ComposeBridges.SnackbarHost(
            hostState: ((Java.Lang.Object)_hostState.Jvm).Handle,
            modifier:  BuildModifier(),
            snackbar:  snackbar,
            composer:  composer);
    }
}
