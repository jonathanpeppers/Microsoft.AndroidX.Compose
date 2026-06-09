namespace Microsoft.AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Inline run style applied to a sub-range of a <see cref="Paragraph"/>.
/// Mirrors upstream's <c>MarkupType</c> enum.
/// </summary>
public enum MarkupType
{
    /// <summary>Tappable link target — rendered with an underline.</summary>
    Link,
    /// <summary>Inline code span — monospace font over a tinted background.</summary>
    Code,
    /// <summary>Italic emphasis.</summary>
    Italic,
    /// <summary>Bold emphasis.</summary>
    Bold,
}
