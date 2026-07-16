using AndroidX.Compose.Foundation.Lazy.Staggeredgrid;

namespace AndroidX.Compose;

/// <summary>
/// Managed column/row-count strategy for
/// <see cref="LazyVerticalStaggeredGrid{T}"/> and
/// <see cref="LazyHorizontalStaggeredGrid{T}"/>:
/// <code>
/// new LazyVerticalStaggeredGrid&lt;int&gt;(StaggeredGridCells.Fixed(2), items, …)
/// new LazyVerticalStaggeredGrid&lt;int&gt;(StaggeredGridCells.Adaptive(120.Dp()), items, …)
/// new LazyVerticalStaggeredGrid&lt;int&gt;(StaggeredGridCells.FixedSize(80.Dp()), items, …)
/// </code>
/// </summary>
public sealed class StaggeredGridCells
{
    internal IStaggeredGridCells Jvm { get; }

    StaggeredGridCells(IStaggeredGridCells jvm)
    {
        Jvm = jvm;
    }

    /// <summary>
    /// Lay out a fixed number of equally-sized cells across the cross
    /// axis (column count for <see cref="LazyVerticalStaggeredGrid{T}"/>,
    /// row count for <see cref="LazyHorizontalStaggeredGrid{T}"/>).
    /// </summary>
    /// <param name="count">Positive number of cells across the cross axis.</param>
    public static StaggeredGridCells Fixed(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Staggered-grid cell count must be positive.");

        return new StaggeredGridCells(new StaggeredGridCellsFixed(count));
    }

    /// <summary>
    /// Lay out as many equally-sized cells as fit, given a minimum cell
    /// size along the cross axis. The Kotlin
    /// <c>StaggeredGridCells.Adaptive(Dp)</c> constructor is stripped
    /// from the binding because <c>Dp</c> is an inline class.
    /// </summary>
    /// <param name="minSize">Positive minimum cross-axis cell size.</param>
    public static StaggeredGridCells Adaptive(Dp minSize)
    {
        if (!(minSize.Value > 0))
            throw new ArgumentOutOfRangeException(nameof(minSize), minSize, "Minimum staggered-grid cell size must be positive.");

        return new StaggeredGridCells(ComposeBridges.StaggeredGridCellsAdaptive(minSize.Value));
    }

    /// <summary>
    /// Lay out cells with a fixed cross-axis size, filling as
    /// many as fit. Like <see cref="Adaptive"/>, the Kotlin
    /// <c>FixedSize(Dp)</c> constructor is stripped (inline-class
    /// <c>Dp</c>).
    /// </summary>
    /// <param name="size">Positive fixed cross-axis cell size.</param>
    public static StaggeredGridCells FixedSize(Dp size)
    {
        if (!(size.Value > 0))
            throw new ArgumentOutOfRangeException(nameof(size), size, "Fixed staggered-grid cell size must be positive.");

        return new StaggeredGridCells(ComposeBridges.StaggeredGridCellsFixedSize(size.Value));
    }
}
