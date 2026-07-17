using NavigationSuiteBindings = AndroidX.Compose.Material3.Adaptive.NavigationSuite;

namespace AndroidX.Compose;

/// <summary>
/// State holder controlling whether a <see cref="NavigationSuiteScaffold"/>
/// navigation component is visible.
/// </summary>
public sealed class NavigationSuiteScaffoldState
{
    internal NavigationSuiteBindings.INavigationSuiteScaffoldState? Jvm;

    /// <summary>Initial visibility remembered during the first composition.</summary>
    public NavigationSuiteBindings.NavigationSuiteScaffoldValue InitialValue { get; }

    /// <summary>Creates a state holder that is initially visible unless specified otherwise.</summary>
    /// <param name="initialValue">Initial scaffold visibility.</param>
    public NavigationSuiteScaffoldState(
        NavigationSuiteBindings.NavigationSuiteScaffoldValue? initialValue = null)
    {
        InitialValue = initialValue
            ?? NavigationSuiteBindings.NavigationSuiteScaffoldValue.Visible
            ?? throw new InvalidOperationException(
                "NavigationSuiteScaffoldValue.Visible was unavailable.");
    }

    /// <summary>Whether a visibility transition is currently running.</summary>
    public bool IsAnimating => Jvm?.IsAnimating ?? false;

    /// <summary>Current scaffold visibility.</summary>
    public NavigationSuiteBindings.NavigationSuiteScaffoldValue CurrentValue =>
        Jvm?.CurrentValue ?? InitialValue;

    /// <summary>Target visibility during a transition.</summary>
    public NavigationSuiteBindings.NavigationSuiteScaffoldValue TargetValue =>
        Jvm?.TargetValue ?? InitialValue;

    /// <summary>Animates the navigation component out of view.</summary>
    /// <param name="cancellationToken">Cancels the returned task and the underlying Kotlin operation.</param>
    public Task HideAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.NavigationSuiteScaffoldStateHide(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Animates the navigation component into view.</summary>
    /// <param name="cancellationToken">Cancels the returned task and the underlying Kotlin operation.</param>
    public Task ShowAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.NavigationSuiteScaffoldStateShow(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Animates to the opposite of the current target visibility.</summary>
    /// <param name="cancellationToken">Cancels the returned task and the underlying Kotlin operation.</param>
    public Task ToggleAsync(CancellationToken cancellationToken = default) =>
        SuspendBridge.Invoke(
            cont => ComposeBridges.NavigationSuiteScaffoldStateToggle(
                ((Java.Lang.Object)RequireJvm()).Handle, cont),
            cancellationToken);

    /// <summary>Snaps immediately to the requested visibility.</summary>
    /// <param name="targetValue">Target scaffold visibility.</param>
    /// <param name="cancellationToken">Cancels the returned task and the underlying Kotlin operation.</param>
    public Task SnapToAsync(
        NavigationSuiteBindings.NavigationSuiteScaffoldValue targetValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetValue);
        return SuspendBridge.Invoke(
            cont => ComposeBridges.NavigationSuiteScaffoldStateSnapTo(
                ((Java.Lang.Object)RequireJvm()).Handle, targetValue, cont),
            cancellationToken);
    }

    NavigationSuiteBindings.INavigationSuiteScaffoldState RequireJvm() =>
        Jvm ?? throw new InvalidOperationException(
            "NavigationSuiteScaffoldState is not bound. Render it with NavigationSuiteScaffold before controlling it.");
}
