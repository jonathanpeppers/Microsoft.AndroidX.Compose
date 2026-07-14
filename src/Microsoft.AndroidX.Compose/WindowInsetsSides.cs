namespace AndroidX.Compose;

/// <summary>
/// Sides selected by <see cref="WindowInsets.Only(WindowInsetsSides)"/>.
/// Mirrors Compose's <c>WindowInsetsSides</c> inline value class.
/// </summary>
[Flags]
public enum WindowInsetsSides
{
    /// <summary>No sides.</summary>
    None = 0,

    /// <summary>The logical start edge, respecting layout direction.</summary>
    Start = 9,

    /// <summary>The logical end edge, respecting layout direction.</summary>
    End = 6,

    /// <summary>The top edge.</summary>
    Top = 16,

    /// <summary>The bottom edge.</summary>
    Bottom = 32,

    /// <summary>The physical left edge.</summary>
    Left = 10,

    /// <summary>The physical right edge.</summary>
    Right = 5,

    /// <summary>All horizontal edges.</summary>
    Horizontal = 15,

    /// <summary>Both vertical edges.</summary>
    Vertical = 48,

    /// <summary>Every edge.</summary>
    All = Horizontal | Vertical,
}
