using Androidx.Compose.Runtime;

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
    internal abstract void Render(IComposer composer);
}
