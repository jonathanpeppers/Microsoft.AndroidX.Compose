using Android.Runtime;
using AndroidX.Compose.Material3.Adaptive.Layout;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 fold-aware list-detail scaffold controlled by a
/// <see cref="ListDetailPaneScaffoldNavigator{T}"/>.
/// </summary>
/// <remarks>
/// Unlike <see cref="NavigableListDetailPaneScaffold{T}"/>, this lower-level
/// scaffold does not install a system or predictive Back handler. Use it when
/// an outer navigation host owns Back and route restoration.
/// </remarks>
/// <typeparam name="T">Type of the navigator's destination content key.</typeparam>
public sealed class ListDetailPaneScaffold<T> : ComposableNode
{
    readonly ListDetailPaneScaffoldNavigator<T> _navigator;

    /// <summary>Creates a scaffold controlled by <paramref name="navigator"/>.</summary>
    public ListDetailPaneScaffold(ListDetailPaneScaffoldNavigator<T> navigator)
    {
        ArgumentNullException.ThrowIfNull(navigator);
        _navigator = navigator;
    }

    /// <summary>Required pane containing selectable list items.</summary>
    public required ComposableNode ListPane { get; set; }

    /// <summary>Required pane containing detail for the selected item.</summary>
    public required ComposableNode DetailPane { get; set; }

    /// <summary>Optional third pane containing additional context.</summary>
    public ComposableNode? ExtraPane { get; set; }

    /// <inheritdoc />
    public override void Render(IComposer composer)
    {
        if (ListPane is null)
            throw new InvalidOperationException(
                "ListDetailPaneScaffold.ListPane is required.");
        if (DetailPane is null)
            throw new InvalidOperationException(
                "ListDetailPaneScaffold.DetailPane is required.");

        var listPane = ListPane;
        var detailPane = DetailPane;
        var extraPane = ExtraPane;
        var list = ComposableLambdas.Wrap3(
            composer,
            (scope, current) => RenderAnimatedPane(
                scope, listPane, current));
        var detail = ComposableLambdas.Wrap3(
            composer,
            (scope, current) => RenderAnimatedPane(
                scope, detailPane, current));
        var extra = extraPane is null
            ? null
            : ComposableLambdas.Wrap3(
                composer,
                (scope, current) => RenderAnimatedPane(
                    scope, extraPane, current));

        var modifier = BuildModifier();
        int defaults = (1 << 6) | (1 << 7);
        if (modifier is null)
            defaults |= 1 << 4;
        if (extra is null)
            defaults |= 1 << 5;

        var navigator = _navigator.RequireJvm();
        ListDetailPaneScaffoldKt.ListDetailPaneScaffold(
            navigator.ScaffoldDirective,
            navigator.ScaffoldValue,
            list,
            detail,
            modifier,
            extra,
            paneExpansionDragHandle: null,
            paneExpansionState: null,
            composer,
            p9: 0,
            _changed: defaults);
    }

    static void RenderAnimatedPane(
        IntPtr scopeHandle,
        ComposableNode content,
        IComposer composer)
    {
        var scope = Java.Lang.Object.GetObject<IExtendedPaneScaffoldPaneScope>(
            scopeHandle,
            JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException(
                "List-detail pane scope was unavailable.");
        var body = ComposableLambdas.Wrap3(
            composer,
            current => content.Render(current));

        const int defaults = 0b1111;
        PaneKt.AnimatedPane(
            scope,
            modifier: null,
            enterTransition: null,
            exitTransition: null,
            boundsAnimationSpec: null,
            body,
            composer,
            p7: 0,
            _changed: defaults);
    }
}
