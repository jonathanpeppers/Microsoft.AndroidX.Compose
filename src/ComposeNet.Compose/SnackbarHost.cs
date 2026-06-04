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
        // The suspending `showSnackbar` trigger on SnackbarHostState isn't
        // bridged yet, so this lambda would only fire if a Java/Kotlin
        // caller enqueued data. Render nothing rather than a blank Snackbar
        // surface — once the `Snackbar(snackbarData)` overload is bridged
        // we can forward to it here.
        var snackbar = new ComposableLambda3((_, _) => { });

        ComposeBridges.SnackbarHost(
            hostState: ((Java.Lang.Object)_hostState.Jvm).Handle,
            modifier:  BuildModifier(),
            snackbar:  snackbar,
            composer:  composer);
    }
}
