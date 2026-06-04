using AndroidX.Compose.Runtime;
using AndroidX.Compose.UI;

namespace ComposeNet;

/// <summary>
/// Base type of every composable in the ComposeNet tree-style facade.
///
/// A <see cref="ComposableNode"/> is a passive AST node: building it
/// (<c>new Text("Hi")</c>, <c>new Column { ... }</c>) does NOT call into
/// Compose. The activity walks the tree and calls <see cref="Render"/>
/// during composition, threading the <see cref="IComposer"/> through
/// container nodes to their children.
///
/// This is the C# moral equivalent of Kotlin Compose's IR transform —
/// the composer is explicit at the implementation layer (honest mirror
/// of <c>ComposerParamTransformer</c>), invisible at the user layer.
/// </summary>
public abstract class ComposableNode
{
    /// <summary>
    /// Optional <see cref="ComposeNet.Modifier"/> chain applied to this
    /// composable. For leaf composables, set via object-initializer:
    /// <code>new Text("Hi") { Modifier = Modifier.Companion.Padding(8) }</code>
    /// For container composables (which use collection-initializer syntax),
    /// add the modifier inline alongside the children:
    /// <code>new Column { Modifier.Companion.Padding(16), new Text("Hi") }</code>
    /// Composables without a Kotlin <c>modifier</c> parameter
    /// (e.g. <see cref="MaterialTheme"/>) ignore this property.
    /// </summary>
    public Modifier? Modifier { get; set; }

    Modifier? _prepended;
    Modifier? _appended;

    /// <summary>
    /// Inject a <see cref="ComposeNet.Modifier"/> at the START of this
    /// node's modifier chain. The prepended ops are consumed (and
    /// cleared) by the next call to <see cref="BuildModifier"/>, so a
    /// parent layout typically calls this immediately before
    /// <see cref="Render"/>. Repeated calls before consumption
    /// accumulate via <see cref="ComposeNet.Modifier.Then"/>.
    ///
    /// Intended use: a parent layout that needs to pass a runtime
    /// modifier into a child without inserting a wrapper layout node —
    /// e.g. <see cref="Scaffold"/> threading
    /// <c>Modifier.padding(paddingValues)</c> into its body so the
    /// body's own modifier chain composes naturally with the inset
    /// padding, mirroring the Kotlin idiom
    /// <c>Column(Modifier.padding(paddingValues)) { ... }</c>.
    /// </summary>
    public void PrependModifier(Modifier modifier)
    {
        System.ArgumentNullException.ThrowIfNull(modifier);
        _prepended = _prepended is null ? modifier : _prepended.Then(modifier);
    }

    /// <summary>
    /// Inject a <see cref="ComposeNet.Modifier"/> at the END of this
    /// node's modifier chain. Mirror of <see cref="PrependModifier"/>;
    /// consumed (and cleared) by the next call to
    /// <see cref="BuildModifier"/>.
    /// </summary>
    public void AppendModifier(Modifier modifier)
    {
        System.ArgumentNullException.ThrowIfNull(modifier);
        _appended = _appended is null ? modifier : _appended.Then(modifier);
    }

    /// <summary>
    /// Materialize <see cref="Modifier"/> for the JNI call, folding in
    /// any pending <see cref="PrependModifier"/> / <see cref="AppendModifier"/>
    /// contributions and clearing them so the next composition starts
    /// fresh. Returns <c>null</c> when the combined chain is empty —
    /// callers should leave the Kotlin <c>$default</c> bit set so
    /// Compose substitutes its real default.
    /// </summary>
    internal IModifier? BuildModifier()
    {
        var prepended = _prepended;
        var appended = _appended;
        _prepended = null;
        _appended = null;

        Modifier? combined = prepended;
        if (Modifier is not null)
            combined = combined is null ? Modifier : combined.Then(Modifier);
        if (appended is not null)
            combined = combined is null ? appended : combined.Then(appended);

        return combined?.Build();
    }

    internal abstract void Render(IComposer composer);
}
