using AndroidX.Compose;
using AndroidX.Compose.Runtime;
using Microsoft.AndroidX.Compose.Maui.Platform;
using Microsoft.Maui.Handlers;
using ComposeColor = AndroidX.Compose.Color;
using ComposeLinearProgressIndicator = AndroidX.Compose.LinearProgressIndicator;

namespace Microsoft.AndroidX.Compose.Maui.Handlers;

/// <summary>
/// MAUI <see cref="Microsoft.Maui.Controls.ProgressBar"/> handler that
/// renders through Jetpack Compose's Material 3
/// <c>LinearProgressIndicator</c> (determinate overload). Replaces
/// MAUI's stock <c>android.widget.ProgressBar</c>-based handler when
/// the consumer calls
/// <see cref="Hosting.AppHostBuilderExtensions.UseAndroidXCompose"/>.
/// </summary>
/// <remarks>
/// <para>Folds into the page's single composition via
/// <see cref="ComposeElementHandler{TVirtualView}"/> /
/// <see cref="IComposeHandler"/>. <see cref="IProgress.Progress"/> is
/// double-typed in MAUI; Compose's <c>LinearProgressIndicator</c>
/// determinate overload takes <see cref="float"/>, so the handler
/// down-casts and clamps to <c>[0, 1]</c>.</para>
///
/// <para>The Compose indicator hugs an intrinsic 4dp height and the
/// caller's measured width. MAUI <c>ProgressBar</c> typically uses
/// <c>HorizontalOptions="Fill"</c>, so the handler unconditionally
/// prepends <c>Modifier.fillMaxWidth()</c>; otherwise the bar would
/// render as a 4dp dot (Compose's intrinsic minimum without a width
/// constraint).</para>
/// </remarks>
public partial class ProgressBarHandler : ComposeElementHandler<IProgress>
{
    /// <summary>
    /// Property mapper that forwards MAUI <see cref="IProgress"/> property
    /// changes to the Compose-backed <see cref="ComposeView"/>.
    /// </summary>
    public static IPropertyMapper<IProgress, ProgressBarHandler> Mapper =
        new PropertyMapper<IProgress, ProgressBarHandler>(ViewHandler.ViewMapper)
        {
            [nameof(IProgress.Progress)]      = MapProgress,
            [nameof(IProgress.ProgressColor)] = MapProgressColor,
        };

    /// <summary>Command mapper (inherits view-level commands; no extras).</summary>
    public static CommandMapper<IProgress, ProgressBarHandler> CommandMapper =
        new(ViewCommandMapper);

    readonly MutableState<float> _progress = new(0f);
    readonly MutableState<long?> _color    = new((long?)null);

    /// <summary>Construct a handler with the default mappers.</summary>
    public ProgressBarHandler() : base(Mapper, CommandMapper) { }

    /// <summary>Construct a handler with custom mappers.</summary>
    public ProgressBarHandler(IPropertyMapper? mapper, CommandMapper? commandMapper = null)
        : base(mapper ?? Mapper, commandMapper ?? CommandMapper) { }

    /// <inheritdoc/>
    public override ComposableNode BuildNode(IComposer composer)
    {
        var packed = _color.Value;
        var bar = new ComposeLinearProgressIndicator
        {
            Progress = _progress.Value,
            Color    = packed.HasValue ? new ComposeColor(packed.Value) : null,
        };
        bar.PrependModifier(Modifier.FillMaxWidth().ApplyGestures(VirtualView!, MauiContext));
        return bar;
    }

    /// <summary>Map <see cref="IProgress.Progress"/> (clamped to 0..1) to the Compose progress slot.</summary>
    public static void MapProgress(ProgressBarHandler handler, IProgress progress) =>
        handler._progress.Value = (float)Math.Clamp(progress.Progress, 0d, 1d);

    /// <summary>Map <see cref="IProgress.ProgressColor"/> to the Compose <c>color</c> slot.</summary>
    public static void MapProgressColor(ProgressBarHandler handler, IProgress progress) =>
        handler._color.Value = ColorMapping.ToPackedLong(progress.ProgressColor);
}
