namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Style of a single paragraph in a <see cref="Post"/> body. Matches the
/// upstream Kotlin sample's <c>ParagraphType</c>; inline ranges are carried
/// separately by <see cref="Markup"/>.
/// </summary>
public enum ParagraphType
{
    /// <summary>Opening title — large, weighty.</summary>
    Title,
    /// <summary>Photo / illustration caption — small, italic-ish.</summary>
    Caption,
    /// <summary>Section header.</summary>
    Header,
    /// <summary>Subsection header.</summary>
    Subhead,
    /// <summary>Body paragraph.</summary>
    Text,
    /// <summary>Single-line monospace code block.</summary>
    CodeBlock,
    /// <summary>Pull-quote.</summary>
    Quote,
    /// <summary>One bullet in a bulleted list.</summary>
    Bullet,
}
