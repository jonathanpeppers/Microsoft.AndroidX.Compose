using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>Snackbar</c>. A transient message bar typically
/// rendered in <see cref="Scaffold.SnackbarHost"/>:
/// <code>
/// new Snackbar
/// {
///     Action = new Button(onClick: ...) { new Text("Undo") },
///     Body   = new Text("Item deleted"),
/// }
/// </code>
/// </summary>
public sealed class Snackbar : ComposableNode
{
    /// <summary>Required: the message body slot.</summary>
    public ComposableNode? Body { get; set; }

    /// <summary>Optional: trailing action button (e.g. "Undo").</summary>
    public ComposableNode? Action { get; set; }

    /// <summary>Optional: dismiss action (e.g. close icon button).</summary>
    public ComposableNode? DismissAction { get; set; }

    internal override void Render(IComposer composer)
    {
        if (Body is null)
            throw new System.InvalidOperationException(
                "Snackbar.Body is required (the Kotlin content lambda has no default).");

        var content = new ComposableLambda2(c => Body.Render(c));
        ComposableLambda2? action = Action is null ? null
            : new ComposableLambda2(c => Action.Render(c));
        ComposableLambda2? dismiss = DismissAction is null ? null
            : new ComposableLambda2(c => DismissAction.Render(c));

        int defaults = (int)SnackbarDefault.All;
        var modifier = BuildModifier();
        if (modifier is not null) defaults &= ~(int)SnackbarDefault.Modifier;
        if (action   is not null) defaults &= ~(int)SnackbarDefault.Action;
        if (dismiss  is not null) defaults &= ~(int)SnackbarDefault.DismissAction;

        ComposeBridges.Snackbar(modifier, action, dismiss, content, defaults, composer);
    }
}
