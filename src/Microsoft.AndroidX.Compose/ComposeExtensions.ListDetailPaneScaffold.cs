using AndroidX.Compose.Material3.Adaptive;
using AndroidX.Compose.Material3.Adaptive.Layout;
using AndroidX.Compose.Material3.Adaptive.Navigation;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

public static partial class ComposeExtensions
{
    /// <summary>
    /// Calculates a fold-aware pane directive from live or simulated adaptive
    /// window information.
    /// </summary>
    /// <param name="windowAdaptiveInfo">
    /// Window size and posture, including separating and occluding hinges.
    /// </param>
    /// <param name="verticalHingePolicy">
    /// Which vertical hinges must be excluded from pane content.
    /// </param>
    /// <param name="twoPanesOnMediumWidth">
    /// Enables two horizontal panes beginning at the medium width class rather
    /// than the standard expanded width class.
    /// </param>
    public static PaneScaffoldDirective CalculateListDetailPaneScaffoldDirective(
        this WindowAdaptiveInfo windowAdaptiveInfo,
        AdaptiveHingePolicy verticalHingePolicy =
            AdaptiveHingePolicy.AvoidSeparating,
        bool twoPanesOnMediumWidth = false)
    {
        ArgumentNullException.ThrowIfNull(windowAdaptiveInfo);
        int policy = verticalHingePolicy switch
        {
            AdaptiveHingePolicy.AlwaysAvoid => 0,
            AdaptiveHingePolicy.AvoidSeparating => 1,
            AdaptiveHingePolicy.AvoidOccluding => 2,
            AdaptiveHingePolicy.NeverAvoid => 3,
            _ => throw new ArgumentOutOfRangeException(
                nameof(verticalHingePolicy),
                verticalHingePolicy,
                "Unknown adaptive hinge policy."),
        };

        return twoPanesOnMediumWidth
            ? PaneScaffoldDirectiveKt
                .CalculatePaneScaffoldDirectiveWithTwoPanesOnMediumWidth(
                    windowAdaptiveInfo,
                    policy)
            : PaneScaffoldDirectiveKt.CalculatePaneScaffoldDirective(
                windowAdaptiveInfo,
                policy);
    }

    /// <summary>
    /// Remembers a fold-aware Material 3 list-detail navigator in the current
    /// composition.
    /// </summary>
    /// <typeparam name="T">
    /// Content-key type. Primitive values, strings, and bound Java peers are
    /// supported. Use a Bundle-saveable key such as <see cref="long"/> or
    /// <see cref="string"/> when destination history must survive activity
    /// recreation.
    /// </typeparam>
    public static ListDetailPaneScaffoldNavigator<T>
        RememberListDetailPaneScaffoldNavigator<T>(
            this IComposer composer,
            AdaptiveHingePolicy verticalHingePolicy =
                AdaptiveHingePolicy.AvoidSeparating,
            bool twoPanesOnMediumWidth = false,
            bool destinationHistoryAware = true,
            WindowAdaptiveInfo? windowAdaptiveInfo = null)
    {
        ArgumentNullException.ThrowIfNull(composer);

        var adaptiveInfo = windowAdaptiveInfo
            ?? composer.CurrentWindowAdaptiveInfo();
        var directive = adaptiveInfo
            .CalculateListDetailPaneScaffoldDirective(
                verticalHingePolicy,
                twoPanesOnMediumWidth);

        // The typed source overload adds initialDestinationHistory after
        // isDestinationHistoryAware. Seed it with the canonical list pane;
        // bit 1 asks Kotlin for the standard list-detail adapt strategies.
        const int defaults = 1 << 1;
        using var initialDestination =
            new ThreePaneScaffoldDestinationItem(
                ListDetailPaneScaffoldNavigator<T>.ToJvmRole(
                    AdaptivePaneRole.List),
                contentKey: null);
        var jvm = ThreePaneScaffoldNavigatorKt
            .RememberListDetailPaneScaffoldNavigator(
                directive,
                adaptStrategies: null,
                destinationHistoryAware,
                initialDestinationHistory: [initialDestination],
                composer,
                p5: 0,
                _changed: defaults);
        var navigator = composer.Remember(
            static () => new ListDetailPaneScaffoldNavigator<T>());
        navigator.Bind(jvm);
        return navigator;
    }
}
