using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Scaffold</c>. Hosts a primary <see cref="Body"/> with
/// optional Material chrome — top app bar, bottom bar, snackbar host,
/// and floating action button — pinned to their conventional edges of
/// the screen.
///
/// The Kotlin overload is stripped from the binding because
/// <c>floatingActionButtonPosition</c> (<c>FabPosition</c>),
/// <c>containerColor</c> and <c>contentColor</c> (<c>Color</c>) are
/// <c>@JvmInline value class</c> parameters; we call it through a JNI
/// bridge in <see cref="ComposeBridges"/> against the mangled name
/// <c>Scaffold-TvnljyQ</c>.
///
/// <code>
/// new Scaffold
/// {
///     TopBar    = new Text("My App"),
///     BottomBar = new NavigationBar { ... },
///     Body      = tabContent,
/// }
/// </code>
/// </summary>
public sealed class Scaffold : ComposableNode
{
    /// <summary>Optional: persistent top app bar slot.</summary>
    public ComposableNode? TopBar { get; set; }

    /// <summary>Optional: persistent bottom bar slot (e.g. <see cref="NavigationBar"/>).</summary>
    public ComposableNode? BottomBar { get; set; }

    /// <summary>Optional: snackbar host slot, typically anchored above <see cref="BottomBar"/>.</summary>
    public ComposableNode? SnackbarHost { get; set; }

    /// <summary>Optional: floating action button slot.</summary>
    public ComposableNode? FloatingActionButton { get; set; }

    /// <summary>Required: the main body, laid out under the top bar and above the bottom bar.</summary>
    public ComposableNode? Body { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Body is null)
            throw new System.InvalidOperationException(
                "Scaffold.Body is required (the Kotlin parameter has no default).");

        var content = new ComposableLambda3(c => Body.Render(c));

        ComposableLambda2? topBar = TopBar is null ? null
            : new ComposableLambda2(c => TopBar.Render(c));
        ComposableLambda2? bottomBar = BottomBar is null ? null
            : new ComposableLambda2(c => BottomBar.Render(c));
        ComposableLambda2? snackbarHost = SnackbarHost is null ? null
            : new ComposableLambda2(c => SnackbarHost.Render(c));
        ComposableLambda2? fab = FloatingActionButton is null ? null
            : new ComposableLambda2(c => FloatingActionButton.Render(c));

        int defaults = (int)ScaffoldDefault.All;
        if (topBar       is not null) defaults &= ~(int)ScaffoldDefault.TopBar;
        if (bottomBar    is not null) defaults &= ~(int)ScaffoldDefault.BottomBar;
        if (snackbarHost is not null) defaults &= ~(int)ScaffoldDefault.SnackbarHost;
        if (fab          is not null) defaults &= ~(int)ScaffoldDefault.FloatingActionButton;

        ComposeBridges.Scaffold(
            topBar:               topBar,
            bottomBar:            bottomBar,
            snackbarHost:         snackbarHost,
            floatingActionButton: fab,
            content:              content,
            defaults:             defaults,
            composer:             composer);
    }
}
