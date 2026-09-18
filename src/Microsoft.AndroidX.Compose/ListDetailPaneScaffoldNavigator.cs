using AndroidX.Compose.Material3.Adaptive.Layout;
using AndroidX.Compose.Material3.Adaptive.Navigation;

namespace AndroidX.Compose;

/// <summary>
/// Composition-owned navigation state for a
/// <see cref="NavigableListDetailPaneScaffold{T}"/>.
/// </summary>
/// <typeparam name="T">Type of the destination content key.</typeparam>
public sealed class ListDetailPaneScaffoldNavigator<T>
{
    IThreePaneScaffoldNavigator? _jvm;

    internal ListDetailPaneScaffoldNavigator() { }

    internal void Bind(IThreePaneScaffoldNavigator jvm)
    {
        ArgumentNullException.ThrowIfNull(jvm);
        _jvm = jvm;
    }

    /// <summary>Whether the navigator currently has a pane destination.</summary>
    public bool HasCurrentDestination => _jvm?.CurrentDestination is not null;

    /// <summary>The currently focused pane, or <c>null</c> before navigation.</summary>
    public AdaptivePaneRole? CurrentPane
    {
        get
        {
            var destination = _jvm?.CurrentDestination;
            return destination is null ? null : FromJvmRole(destination.Pane);
        }
    }

    /// <summary>
    /// The current destination key, or the default value of
    /// <typeparamref name="T"/> when no destination or key is present.
    /// </summary>
    public T? CurrentContentKey
    {
        get
        {
            var destination = _jvm?.CurrentDestination;
            if (destination is null)
                return default;
            var contentKey = destination.ContentKey;
            bool retained = false;
            try
            {
                var value = MutableState<T>.FromJava(contentKey);
                retained = ReferenceEquals(value, contentKey);
                return value;
            }
            finally
            {
                if (!retained)
                    contentKey?.Dispose();
            }
        }
    }

    /// <summary>The current fold-aware pane arrangement directive.</summary>
    public PaneScaffoldDirective ScaffoldDirective => RequireJvm().ScaffoldDirective;

    /// <summary>Whether predictive Back is currently seeking between destinations.</summary>
    public bool IsPredictiveBackInProgress =>
        _jvm?.ScaffoldState.IsPredictiveBackInProgress ?? false;

    /// <summary>Whether Back can consume a pane destination.</summary>
    public bool CanNavigateBack(
        PaneBackNavigationBehavior behavior =
            PaneBackNavigationBehavior.PopUntilScaffoldValueChange) =>
        RequireJvm().CanNavigateBack(ToJvmBehavior(behavior));

    /// <summary>Navigates to a pane and optional content key.</summary>
    /// <param name="pane">Destination pane.</param>
    /// <param name="contentKey">Content selected in the destination pane.</param>
    /// <param name="cancellationToken">
    /// Cancels the returned task. The underlying Kotlin operation continues
    /// to natural completion.
    /// </param>
    public Task NavigateToAsync(
        AdaptivePaneRole pane,
        T? contentKey = default,
        CancellationToken cancellationToken = default)
    {
        var jvm = RequireJvm();
        var boxed = contentKey is null
            ? null
            : MutableState<T>.ToJava(contentKey);
        try
        {
            return SuspendBridge.Invoke(
                cont => ComposeBridges.ThreePaneScaffoldNavigatorNavigateTo(
                    ((Java.Lang.Object)jvm).Handle,
                    ToJvmRole(pane),
                    boxed,
                    cont),
                cancellationToken);
        }
        finally
        {
            if (contentKey is not Java.Lang.Object)
                boxed?.Dispose();
        }
    }

    /// <summary>
    /// Navigates Back through pane destination history and returns whether a
    /// previous destination was available.
    /// </summary>
    /// <param name="behavior">The history entries Back should skip.</param>
    /// <param name="cancellationToken">
    /// Cancels the returned task. The underlying Kotlin operation continues
    /// to natural completion.
    /// </param>
    public Task<bool> NavigateBackAsync(
        PaneBackNavigationBehavior behavior =
            PaneBackNavigationBehavior.PopUntilScaffoldValueChange,
        CancellationToken cancellationToken = default)
    {
        var jvm = RequireJvm();
        return SuspendBridge.Invoke(
            cont => ComposeBridges.ThreePaneScaffoldNavigatorNavigateBack(
                ((Java.Lang.Object)jvm).Handle,
                ToJvmBehavior(behavior),
                cont),
            static boxed => boxed is Java.Lang.Boolean result
                ? result.BooleanValue()
                : throw new InvalidCastException(
                    $"Expected java.lang.Boolean; got '{boxed?.Class?.Name ?? "null"}'."),
            cancellationToken);
    }

    internal IThreePaneScaffoldNavigator RequireJvm() =>
        _jvm ?? throw new InvalidOperationException(
            "ListDetailPaneScaffoldNavigator requires a live composition owner. Call RememberListDetailPaneScaffoldNavigator inside composition before using it.");

    internal static ThreePaneScaffoldRole ToJvmRole(AdaptivePaneRole role)
    {
        var roles = ListDetailPaneScaffoldRole.Instance;
        return role switch
        {
            AdaptivePaneRole.List => roles.List,
            AdaptivePaneRole.Detail => roles.Detail,
            AdaptivePaneRole.Extra => roles.Extra,
            _ => throw new ArgumentOutOfRangeException(
                nameof(role), role, "Unknown adaptive pane role."),
        };
    }

    internal static string ToJvmBehavior(PaneBackNavigationBehavior behavior) =>
        behavior switch
        {
            PaneBackNavigationBehavior.PopLatest => "PopLatest",
            PaneBackNavigationBehavior.PopUntilScaffoldValueChange =>
                "PopUntilScaffoldValueChange",
            PaneBackNavigationBehavior.PopUntilCurrentDestinationChange =>
                "PopUntilCurrentDestinationChange",
            PaneBackNavigationBehavior.PopUntilContentChange =>
                "PopUntilContentChange",
            _ => throw new ArgumentOutOfRangeException(
                nameof(behavior),
                behavior,
                "Unknown pane Back navigation behavior."),
        };

    static AdaptivePaneRole FromJvmRole(ThreePaneScaffoldRole role)
    {
        var roles = ListDetailPaneScaffoldRole.Instance;
        if (role.Equals(roles.List))
            return AdaptivePaneRole.List;
        if (role.Equals(roles.Detail))
            return AdaptivePaneRole.Detail;
        if (role.Equals(roles.Extra))
            return AdaptivePaneRole.Extra;
        throw new InvalidOperationException(
            $"Unknown list-detail pane role '{role}'.");
    }
}
