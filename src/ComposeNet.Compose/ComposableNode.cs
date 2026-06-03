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

    /// <summary>
    /// Materialize <see cref="Modifier"/> for the JNI call. Returns
    /// <c>null</c> when the user did not supply a modifier OR supplied
    /// the empty <see cref="ComposeNet.Modifier.Companion"/> — in both
    /// cases callers should leave the Kotlin <c>$default</c> bit set so
    /// Compose substitutes its real default.
    /// </summary>
    internal IModifier? BuildModifier() => Modifier?.Build();

    internal abstract void Render(IComposer composer);
}
