using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeCircularProgressIndicator = AndroidX.Compose.CircularProgressIndicator;
using ComposeColor = AndroidX.Compose.Color;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.ActivityIndicator"/> handler
/// that renders through Jetpack Compose's Material 3
/// <c>CircularProgressIndicator</c> (indeterminate overload). Replaces
/// MAUI's stock <c>android.widget.ProgressBar</c>-based handler when
/// the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. The MAUI
/// <see cref="IActivityIndicator.IsRunning"/> flag toggles the indicator
/// in/out of the composition: when <c>true</c>, the handler emits a
/// <c>CircularProgressIndicator</c>; when <c>false</c>, an empty
/// <c>Box</c>. Conditional emission keeps Compose's animation
/// machinery completely off when <c>IsRunning</c> is <c>false</c>
/// (the indeterminate animator only spins while the indicator is in
/// the composition).</para>
/// </remarks>
public partial class ActivityIndicatorHandler : ComposeElementHandler<IActivityIndicator>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IActivityIndicator"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IActivityIndicator, ActivityIndicatorHandler> Mapper =
        new PropertyMapper<IActivityIndicator, ActivityIndicatorHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IActivityIndicator.IsRunning)] = MapIsRunning,
            [nameof(IActivityIndicator.Color)]     = MapColor,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IActivityIndicator, ActivityIndicatorHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<bool>  _isRunning = new(false);
    readonly MutableState<long?> _color     = new((long?)null);

    /// <summary>Construct a handler with the default mappers.</summary>
    public ActivityIndicatorHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ActivityIndicatorHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var gestureModifier = Modifier.Companion.ApplyGestures(VirtualView!, MauiContext);
        if (!_isRunning.Value)
            return new Box { Modifier = gestureModifier };

        var packed = _color.Value;
        return new ComposeCircularProgressIndicator
        {
            Color = packed.HasValue ? new ComposeColor(packed.Value) : null,
            Modifier = gestureModifier,
        };
    }

    /// <summary>Map <see cref="IActivityIndicator.IsRunning"/> to a presence toggle.</summary>
    public static void MapIsRunning(ActivityIndicatorHandler handler, IActivityIndicator indicator) =>
        handler._isRunning.Value = indicator.IsRunning;

    /// <summary>Map <see cref="IActivityIndicator.Color"/> to the Compose <c>color</c> slot.</summary>
    public static void MapColor(ActivityIndicatorHandler handler, IActivityIndicator indicator) =>
        handler._color.Value = ColorMapping.ToPackedLong(indicator.Color);
}
