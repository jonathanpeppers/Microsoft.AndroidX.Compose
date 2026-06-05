namespace ComposeNet;

/// <summary>
/// Convenience constructors for the <see cref="ComposeNet.Dp"/>
/// struct: <c>16.Dp()</c>, <c>0.5f.Dp()</c>.
/// </summary>
public static class DpExtensions
{
    /// <summary>Wrap an integer dp value in a <c>Dp</c>.</summary>
    public static Dp Dp(this int value) => new(value);

    /// <summary>Wrap a float dp value in a <c>Dp</c>.</summary>
    public static Dp Dp(this float value) => new(value);
}
