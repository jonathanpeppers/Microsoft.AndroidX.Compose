namespace ComposeNet;

/// <summary>
/// Convenience constructors for <see cref="Em"/>: <c>1.2f.Em()</c>.
/// </summary>
public static class EmExtensions
{
    /// <summary>Wrap a float in an <see cref="Em"/>.</summary>
    public static Em Em(this float value) => new(value);
}
