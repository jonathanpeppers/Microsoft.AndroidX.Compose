using System.Collections.Generic;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// One paragraph of <see cref="Post"/> body content. Inline run styling
/// (Link / Bold / Italic / Code spans within a paragraph) is carried by
/// the optional <paramref name="Markups"/> list — non-empty paragraphs
/// render via <see cref="ComposeNet.AnnotatedText"/> using a
/// <see cref="ComposeNet.AnnotatedStringBuilder"/>.
/// </summary>
/// <param name="Type">How to render the text.</param>
/// <param name="Text">The paragraph content.</param>
/// <param name="Markups">Per-run style overlays applied to sub-ranges
/// of <paramref name="Text"/>. <see langword="null"/> or empty means
/// the paragraph renders uniformly.</param>
public sealed record Paragraph(
    ParagraphType Type,
    string Text,
    IReadOnlyList<Markup>? Markups = null);
