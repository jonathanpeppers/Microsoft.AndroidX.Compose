using AndroidX.Compose.Foundation.Lazy.Grid;

namespace AndroidX.Compose;

/// <summary>
/// Managed column/row-count strategy for <see cref="LazyVerticalGrid{T}"/>
/// and <see cref="LazyHorizontalGrid{T}"/>. Use <see cref="Fixed"/> for a
/// fixed cross-axis count or <see cref="Adaptive"/> for a minimum cell size:
/// <code>
/// new LazyVerticalGrid&lt;int&gt;(GridCells.Fixed(3), items, i =&gt; new Text($"{i}"))
/// new LazyVerticalGrid&lt;int&gt;(GridCells.Adaptive(80.Dp()), items, i =&gt; new Text($"{i}"))
/// </code>
/// </summary>
public sealed class GridCells
{
    internal IGridCells Jvm { get; }

    GridCells(IGridCells jvm)
    {
        Jvm = jvm;
    }

    /// <summary>
    /// Lay out a fixed number of equally-sized cells across the cross
    /// axis (column count for <see cref="LazyVerticalGrid{T}"/>, row
    /// count for <see cref="LazyHorizontalGrid{T}"/>).
    /// </summary>
    /// <param name="count">Positive number of cells across the cross axis.</param>
    public static GridCells Fixed(int count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Grid cell count must be positive.");

        return new GridCells(new GridCellsFixed(count));
    }

    /// <summary>
    /// Lay out as many equally-sized cells as fit, given a minimum cell
    /// size along the cross axis. The Kotlin
    /// <c>GridCells.Adaptive(Dp)</c> constructor is stripped from the
    /// binding because <c>Dp</c> is an inline class.
    /// </summary>
    /// <param name="minSize">Positive minimum cross-axis cell size.</param>
    public static GridCells Adaptive(Dp minSize)
    {
        if (!(minSize.Value > 0))
            throw new ArgumentOutOfRangeException(nameof(minSize), minSize, "Minimum grid cell size must be positive.");

        return new GridCells(ComposeBridges.GridCellsAdaptive(minSize.Value));
    }
}
