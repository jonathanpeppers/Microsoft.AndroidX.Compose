using AndroidX.Compose.Material3.PullToRefresh;
using AndroidX.Compose.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// Material 3 pull-to-refresh indicator (the spinning arrow / progress
/// glyph that appears as the user drags), exposed as a
/// <see cref="ComposableNode"/> so it can be plugged into
/// <see cref="PullToRefreshBox.Indicator"/>.
/// </summary>
/// <remarks>
/// <para>Wraps the bound
/// <see cref="PullToRefreshDefaults"/>.<c>Instance.Indicator(...)</c>
/// helper. The indicator must share its <see cref="PullToRefreshState"/>
/// with the parent <see cref="PullToRefreshBox"/> — pass the same
/// wrapper instance to both, like:</para>
/// <code>
/// var state = new PullToRefreshState();
/// new PullToRefreshBox(isRefreshing, onRefresh, state: state)
/// {
///     Indicator = new PullToRefreshIndicator(state, isRefreshing) { Color = Color.Red },
/// };
/// </code>
/// <para>Material 3's stock indicator picks up colors from the active
/// <c>ColorScheme</c>; supply <see cref="ContainerColor"/> /
/// <see cref="Color"/> only to override one (or both) of those slots.
/// Any color left <see langword="null"/> falls through to the theme default.</para>
/// </remarks>
public sealed class PullToRefreshIndicator : ComposableNode
{
    // PullToRefreshDefaults.Indicator $default mask bits — order
    // matches the bound JNI signature
    // (state, isRefreshing, modifier, containerColor, color, maxDistance).
    // Bits 0/1 are always supplied (state, isRefreshing).
    const int BitModifier       = 1 << 2;
    const int BitContainerColor = 1 << 3;
    const int BitColor          = 1 << 4;
    const int BitMaxDistance    = 1 << 5;

    readonly PullToRefreshState _state;
    readonly bool               _isRefreshing;

    /// <summary>
    /// Construct an indicator bound to <paramref name="state"/> (which
    /// must be the same instance handed to the parent
    /// <see cref="PullToRefreshBox"/>) and tracking
    /// <paramref name="isRefreshing"/>.
    /// </summary>
    public PullToRefreshIndicator(PullToRefreshState state, bool isRefreshing)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state        = state;
        _isRefreshing = isRefreshing;
    }

    /// <summary>
    /// Optional background color. <see langword="null"/> falls through to
    /// <c>ColorScheme.surfaceContainerHighest</c>.
    /// </summary>
    public Color? ContainerColor { get; set; }

    /// <summary>
    /// Optional glyph color. <see langword="null"/> falls through to
    /// <c>ColorScheme.onSurfaceVariant</c>.
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// Optional drag-distance threshold in dp. <c>null</c> falls through
    /// to Material's default (<c>80.dp</c>). Increase for sliders /
    /// dense lists where the gesture region competes with content.
    /// </summary>
    public Dp? MaxDistance { get; set; }

    /// <inheritdoc/>
    public override void Render(IComposer composer)
    {
        var jvm = _state.Jvm
            ?? throw new InvalidOperationException(
                "PullToRefreshIndicator's state is not bound. " +
                "Pass the same PullToRefreshState wrapper to PullToRefreshBox " +
                "(it populates state.Jvm during the box's first render).");

        int mask = 0;
        var modifier = BuildModifier();
        if (modifier is null)         mask |= BitModifier;
        if (ContainerColor is null)   mask |= BitContainerColor;
        if (Color          is null)   mask |= BitColor;
        if (MaxDistance    is null)   mask |= BitMaxDistance;

        // $changed bitmask. Bit positions over user params:
        //   1  = state          (Jvm reference DiffSlot — stable when caller
        //                        reuses the same PullToRefreshState wrapper)
        //   4  = isRefreshing   (bool)
        //   7  = modifier       (DiffSlot of structural key)
        //   10 = containerColor (long)
        //   13 = color          (long)
        //   16 = maxDistance    (Dp? — null reads as ChangedBits.Same after
        //                        the first pass via the auto-mask)
        int __changed = 0;
        __changed |= composer.DiffSlot(jvm,                            1);
        __changed |= composer.DiffSlot(_isRefreshing,                  4);
        __changed |= composer.DiffSlot(BuildModifierStructuralKey(),   7);
        __changed |= composer.DiffSlot(ContainerColor,                 10);
        __changed |= composer.DiffSlot(Color,                          13);
        __changed |= composer.DiffSlot(MaxDistance,                    16);

        PullToRefreshDefaults.Instance.Indicator(
            state:          jvm,
            isRefreshing:   _isRefreshing,
            modifier:       modifier,
            containerColor: ContainerColor?.ToPacked() ?? 0L,
            color:          Color?.ToPacked()          ?? 0L,
            maxDistance:    MaxDistance?.Value ?? 0f,
            _composer:      composer,
            p7:             __changed,
            _changed:       mask);
    }
}
