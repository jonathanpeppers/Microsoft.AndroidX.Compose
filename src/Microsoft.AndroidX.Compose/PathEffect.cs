using BoundPathEffect = AndroidX.Compose.UI.Graphics.IPathEffect;
using BoundPathEffectFactory = AndroidX.Compose.UI.Graphics.PathEffect;

namespace AndroidX.Compose;

/// <summary>Reusable geometry effect applied when stroking paths and lines.</summary>
public sealed class PathEffect : IDisposable
{
    BoundPathEffect? _jvm;

    internal BoundPathEffect Jvm => _jvm
        ?? throw new ObjectDisposedException(nameof(PathEffect));

    PathEffect(BoundPathEffect jvm) => _jvm = jvm;

    /// <summary>Creates alternating visible and skipped stroke intervals.</summary>
    public static PathEffect Dash(float[] intervals, float phase = 0f)
    {
        ArgumentNullException.ThrowIfNull(intervals);
        if (intervals.Length < 2 || intervals.Length % 2 != 0)
            throw new ArgumentException(
                "Dash intervals must contain an even number of values and at least two entries.",
                nameof(intervals));
        if (intervals.Any(value => value <= 0f || !float.IsFinite(value)))
            throw new ArgumentOutOfRangeException(
                nameof(intervals), "Dash intervals must be finite and greater than zero.");

        var effect = BoundPathEffectFactory.Companion.DashPathEffect(intervals, phase)
            ?? throw new InvalidOperationException("Compose dash PathEffect factory returned null.");
        return new PathEffect(effect);
    }

    /// <summary>Rounds sharp corners by the supplied pixel radius.</summary>
    public static PathEffect Corner(float radius)
    {
        if (radius < 0f || !float.IsFinite(radius))
            throw new ArgumentOutOfRangeException(nameof(radius));
        var effect = BoundPathEffectFactory.Companion.CornerPathEffect(radius)
            ?? throw new InvalidOperationException("Compose corner PathEffect factory returned null.");
        return new PathEffect(effect);
    }

    /// <summary>Applies <paramref name="inner"/> and then <paramref name="outer"/>.</summary>
    public static PathEffect Chain(PathEffect outer, PathEffect inner)
    {
        ArgumentNullException.ThrowIfNull(outer);
        ArgumentNullException.ThrowIfNull(inner);
        var effect = BoundPathEffectFactory.Companion.ChainPathEffect(outer.Jvm, inner.Jvm)
            ?? throw new InvalidOperationException("Compose chained PathEffect factory returned null.");
        return new PathEffect(effect);
    }

    /// <summary>Stamps a path repeatedly along stroked geometry.</summary>
    public static PathEffect Stamped(
        Path shape,
        float advance,
        float phase = 0f,
        StampedPathEffectStyle style = StampedPathEffectStyle.Translate)
    {
        ArgumentNullException.ThrowIfNull(shape);
        if (advance <= 0f || !float.IsFinite(advance))
            throw new ArgumentOutOfRangeException(nameof(advance));
        if (!float.IsFinite(phase))
            throw new ArgumentOutOfRangeException(nameof(phase));
        var effect = BoundPathEffectFactory.Companion.StampedPathEffect_7aD1DOk(
            shape.Jvm, advance, phase, (int)style)
            ?? throw new InvalidOperationException("Compose stamped PathEffect factory returned null.");
        return new PathEffect(effect);
    }

    /// <summary>Releases the underlying Compose path-effect peer.</summary>
    public void Dispose()
    {
        var peer = _jvm;
        _jvm = null;
        peer?.Dispose();
    }
}
