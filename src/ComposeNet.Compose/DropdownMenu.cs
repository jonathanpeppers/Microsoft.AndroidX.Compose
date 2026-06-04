using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>DropdownMenu</c> — the popup that anchors
/// <see cref="DropdownMenuItem"/> children. Pair with a trigger
/// (commonly an <see cref="IconButton"/>) and a <see cref="MutableState{T}"/>
/// to control visibility:
/// <code>
/// var open = Remember(() =&gt; new MutableState&lt;bool&gt;(false));
///
/// new Box
/// {
///     new IconButton(onClick: () =&gt; open.Value = true)
///         { new Text("⋮") },
///     new DropdownMenu(expanded: open.Value, onDismissRequest: () =&gt; open.Value = false)
///     {
///         new DropdownMenuItem(
///             text:    new Text("Refresh"),
///             onClick: () =&gt; { open.Value = false; ... }),
///     },
/// }
/// </code>
/// </summary>
public sealed class DropdownMenu : ComposableContainer
{
    readonly bool _expanded;
    readonly System.Action _onDismissRequest;

    public DropdownMenu(bool expanded, System.Action onDismissRequest)
    {
        _expanded = expanded;
        _onDismissRequest = onDismissRequest;
    }

    internal override void Render(IComposer composer)
    {
        var onDismiss = new ComposableLambda0(_onDismissRequest);
        var content   = new ComposableLambda3(c => RenderChildren(c));
        ComposeBridges.DropdownMenu(_expanded, onDismiss, BuildModifier(), content, composer);
    }
}
