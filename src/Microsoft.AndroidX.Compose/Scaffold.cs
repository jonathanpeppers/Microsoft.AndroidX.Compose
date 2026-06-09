using global::AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose;

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

    public override void Render(IComposer composer)
    {
        if (Body is null)
            throw new InvalidOperationException(
                "Scaffold.Body is required (the Kotlin parameter has no default).");

        // Material 3's Scaffold passes PaddingValues as the first arg of
        // its content lambda — body must apply it to avoid rendering
        // behind the top/bottom bars. Forward the raw handle directly
        // through ComposableNode.Render(IComposer, IntPtr) so body's
        // BuildModifier can prepend a `Modifier.padding(values)` op via
        // JNI without allocating a managed Modifier wrapper per measure
        // pass (issue #46). ScaffoldLayout is a SubcomposeLayout — this
        // lambda runs on every measure pass, so the saved allocation
        // adds up quickly during scrolls, rotations, and IME animations.
        var body = Body;
        var content = ComposableLambdas.Wrap3(composer, (paddingHandle, c) =>
            body.Render(c, paddingHandle));

        // Always pass non-null slot lambdas. Toggling between Compose's
        // default `{}` (a synthetic Java SAM lambda) and our IFunction2
        // identity confuses M3's internal `rememberComposableLambda`,
        // which casts the slot value to ComposableLambdaImpl on every
        // recomposition — a flip in slot type triggers a runtime
        // ClassCastException. Emit nothing when the user didn't supply
        // a slot; functionally identical to M3's `{}` default.
        var topBar = ComposableLambdas.Wrap2(composer, c => TopBar?.Render(c));
        var bottomBar = ComposableLambdas.Wrap2(composer, c => BottomBar?.Render(c));
        var snackbarHost = ComposableLambdas.Wrap2(composer, c => SnackbarHost?.Render(c));
        var fab = ComposableLambdas.Wrap2(composer, c => FloatingActionButton?.Render(c));

        int defaults = (int)ScaffoldDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)ScaffoldDefault.Modifier;
        defaults &= ~(int)ScaffoldDefault.TopBar;
        defaults &= ~(int)ScaffoldDefault.BottomBar;
        defaults &= ~(int)ScaffoldDefault.SnackbarHost;
        defaults &= ~(int)ScaffoldDefault.FloatingActionButton;

        ComposeBridges.Scaffold(
            modifier:             modifier,
            topBar:               topBar,
            bottomBar:            bottomBar,
            snackbarHost:         snackbarHost,
            floatingActionButton: fab,
            content:              content,
            defaults:             defaults,
            composer:             composer);
    }
}
