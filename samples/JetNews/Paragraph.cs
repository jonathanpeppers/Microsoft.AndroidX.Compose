namespace ComposeNet.Samples.JetNews;

/// <summary>
/// One paragraph of <see cref="Post"/> body content. The upstream
/// sample also carries an inline-<c>markups</c> list (Link/Bold/Italic/
/// Code spans within a paragraph) — that's gated on
/// <c>AnnotatedString</c> binding work and is omitted here.
/// </summary>
/// <param name="Type">How to render the text.</param>
/// <param name="Text">The paragraph content.</param>
public sealed record Paragraph(ParagraphType Type, string Text);
