namespace ComposeNet;

/// <summary>
/// Convenience constructors for the <see cref="ComposeNet.Sp"/>
/// struct: <c>16.Sp()</c>, <c>14.5f.Sp()</c>.
/// </summary>
public static class SpExtensions
{
    /// <summary>Wrap an integer point size in an <c>Sp</c>.</summary>
    public static Sp Sp(this int value) => new(value);

    /// <summary>Wrap a float point size in an <c>Sp</c>.</summary>
    public static Sp Sp(this float value) => new(value);
}
