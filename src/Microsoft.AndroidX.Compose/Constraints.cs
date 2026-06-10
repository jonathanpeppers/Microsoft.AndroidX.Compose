namespace AndroidX.Compose;

/// <summary>
/// Layout-time constraints handed to a <see cref="Layout"/>
/// measure-policy callback. Mirrors
/// <c>AndroidX.Compose.UI.Unit.Constraints</c> — Compose's packed
/// <c>(minWidth, maxWidth, minHeight, maxHeight)</c> tuple in pixels.
/// </summary>
/// <remarks>
/// <para>
/// All four bounds are pixel values, not dp. Multiply / divide by
/// <c>density</c> when you need to round-trip through dp.
/// </para>
/// <para>
/// <see cref="MaxWidth"/> / <see cref="MaxHeight"/> can be unbounded —
/// check <see cref="HasBoundedWidth"/> / <see cref="HasBoundedHeight"/>
/// before comparing the maximum to a fixed pixel threshold. Compose's
/// internal sentinel for "unbounded" is <see cref="int.MaxValue"/>; the
/// helper accessors hide that detail.
/// </para>
/// <para>
/// Construction from a raw packed value is internal — you only ever
/// receive a <see cref="Constraints"/> instance from the runtime, never
/// build one yourself. The struct is cheap (just the packed long), so
/// passing it by value through user code is fine.
/// </para>
/// </remarks>
public readonly struct Constraints
{
    /// <summary>The packed <c>long</c> bit pattern Compose uses internally.</summary>
    internal long Value { get; }

    internal Constraints(long value) => Value = value;

    /// <summary>
    /// Build a <see cref="Constraints"/> envelope to pass to
    /// <see cref="Measurable.Measure"/>. Pass <see cref="int.MaxValue"/>
    /// for an unbounded dimension. The arguments must satisfy
    /// <c>0 ≤ min ≤ max</c> on each axis or Compose will throw.
    /// </summary>
    public static Constraints Create(int minWidth, int maxWidth, int minHeight, int maxHeight)
        => new(AndroidX.Compose.UI.Unit.ConstraintsKt.Constraints(
            minWidth, maxWidth, minHeight, maxHeight));

    /// <summary>
    /// Build a <see cref="Constraints"/> that fixes width to
    /// <paramref name="width"/> and leaves height unbounded.
    /// </summary>
    public static Constraints FixedWidth(int width)
        => Create(width, width, 0, int.MaxValue);

    /// <summary>
    /// Build a <see cref="Constraints"/> that fixes height to
    /// <paramref name="height"/> and leaves width unbounded.
    /// </summary>
    public static Constraints FixedHeight(int height)
        => Create(0, int.MaxValue, height, height);

    /// <summary>Minimum width the layout may take, in pixels. Always finite.</summary>
    public int MinWidth => ComposeBridges.ConstraintsGetMinWidth(Value);

    /// <summary>
    /// Maximum width the layout may take, in pixels. Returns
    /// <see cref="int.MaxValue"/> when the parent imposes no upper bound;
    /// gate on <see cref="HasBoundedWidth"/> before comparing.
    /// </summary>
    public int MaxWidth => ComposeBridges.ConstraintsGetMaxWidth(Value);

    /// <summary>Minimum height the layout may take, in pixels. Always finite.</summary>
    public int MinHeight => ComposeBridges.ConstraintsGetMinHeight(Value);

    /// <summary>
    /// Maximum height the layout may take, in pixels. Returns
    /// <see cref="int.MaxValue"/> when the parent imposes no upper bound;
    /// gate on <see cref="HasBoundedHeight"/> before comparing.
    /// </summary>
    public int MaxHeight => ComposeBridges.ConstraintsGetMaxHeight(Value);

    /// <summary><c>true</c> when <see cref="MaxWidth"/> is finite.</summary>
    public bool HasBoundedWidth => ComposeBridges.ConstraintsHasBoundedWidth(Value);

    /// <summary><c>true</c> when <see cref="MaxHeight"/> is finite.</summary>
    public bool HasBoundedHeight => ComposeBridges.ConstraintsHasBoundedHeight(Value);

    /// <summary>
    /// Return a new <see cref="Constraints"/> with the same min/max heights
    /// and minimum width, but <paramref name="maxWidth"/> as the new
    /// maximum width. Mirrors Kotlin's <c>Constraints.copy(maxWidth = …)</c>.
    /// If the new max is below the existing <see cref="MinWidth"/> the
    /// minimum is lowered to match — Compose throws otherwise.
    /// </summary>
    public Constraints WithMaxWidth(int maxWidth)
        => Create(Math.Min(MinWidth, maxWidth), maxWidth, MinHeight, MaxHeight);

    /// <summary>
    /// Return a new <see cref="Constraints"/> with the same min/max widths
    /// and minimum height, but <paramref name="maxHeight"/> as the new
    /// maximum height. Mirrors Kotlin's <c>Constraints.copy(maxHeight = …)</c>.
    /// If the new max is below the existing <see cref="MinHeight"/> the
    /// minimum is lowered to match — Compose throws otherwise.
    /// </summary>
    public Constraints WithMaxHeight(int maxHeight)
        => Create(MinWidth, MaxWidth, Math.Min(MinHeight, maxHeight), maxHeight);

    /// <summary>
    /// Clamp <paramref name="width"/> into <c>[MinWidth, MaxWidth]</c>.
    /// Mirrors Kotlin's <c>Constraints.constrainWidth(value)</c>.
    /// </summary>
    public int ConstrainWidth(int width) => Math.Clamp(width, MinWidth, MaxWidth);

    /// <summary>
    /// Clamp <paramref name="height"/> into <c>[MinHeight, MaxHeight]</c>.
    /// Mirrors Kotlin's <c>Constraints.constrainHeight(value)</c>.
    /// </summary>
    public int ConstrainHeight(int height) => Math.Clamp(height, MinHeight, MaxHeight);
}
