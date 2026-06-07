using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>DockedSearchBar</c> — the boolean-state docked variant.
/// The Material 3 1.4 design replaces this with the state-based
/// <see cref="SearchBar"/> + <see cref="ExpandedDockedSearchBar"/> pair
/// (sharing a <see cref="SearchBarState"/>); the underlying Kotlin
/// composable is annotated <c>@Deprecated</c> and only the boolean
/// variant is exposed by Material 3 1.4.0.3 because the new design has
/// no standalone "DockedSearchBar" composable.
/// </summary>
/// <remarks>
/// <see cref="InputField"/> is required (the Kotlin parameter has no
/// default) and is rendered at the top of the bar; the
/// collection-initialized children render as the result list inside the
/// docked popup (the underlying Kotlin <c>content</c> lambda receives a
/// <c>ColumnScope</c>).
/// </remarks>
[System.Obsolete("Use the state-based SearchBar + ExpandedDockedSearchBar pair instead.")]
public sealed class DockedSearchBar : ComposableContainer
{
    readonly bool _expanded;
    readonly System.Action<bool> _onExpandedChange;

    public DockedSearchBar(bool expanded, System.Action<bool> onExpandedChange)
    {
        _expanded = expanded;
        _onExpandedChange = onExpandedChange;
    }

    /// <summary>Required: composable that renders the search input field at the top of the bar.</summary>
    public ComposableNode? InputField { get; set; }

    public override void Render(IComposer composer)
    {
        if (InputField is null)
            throw new System.InvalidOperationException(
                "DockedSearchBar.InputField is required (the Kotlin parameter has no default).");

        var inputField       = ComposableLambdas.Wrap2(composer, c => InputField.Render(c));
        var onExpandedChange = new ComposableLambda1(o =>
            _onExpandedChange(((Java.Lang.Boolean)o!).BooleanValue()));
        var content          = ComposableLambdas.Wrap3(composer, c => RenderChildren(c));

        ComposeBridges.DockedSearchBar(
            inputField:       inputField,
            expanded:         _expanded,
            onExpandedChange: onExpandedChange,
            modifier:         BuildModifier(),
            content:          content,
            composer:         composer);
    }
}
