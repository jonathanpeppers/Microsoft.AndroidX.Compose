using ComposeNet;

namespace ComposeNet.Samples.JetNews;

/// <summary>
/// Paragraph rendering for the article screen — one factory per
/// <see cref="ParagraphType"/>. The upstream sample applies inline
/// run styling (Link / Code / Italic / Bold spans inside a paragraph)
/// through Compose's <c>AnnotatedString</c>; that's omitted here
/// because <c>AnnotatedString</c> isn't bound yet.
/// </summary>
internal static class PostBody
{
    static readonly Color Subtle = Color.FromHex("#666666");
    static readonly Color QuoteBar = Color.FromHex("#BBBBBB");
    static readonly Color CodeBg   = Color.FromHex("#F1F3F5");

    public static ComposableNode BuildParagraph(Paragraph p) => p.Type switch
    {
        ParagraphType.Title     => Title(p.Text),
        ParagraphType.Caption   => Caption(p.Text),
        ParagraphType.Header    => Header(p.Text),
        ParagraphType.Subhead   => Subhead(p.Text),
        ParagraphType.Text      => Body(p.Text),
        ParagraphType.CodeBlock => CodeBlock(p.Text),
        ParagraphType.Quote     => Quote(p.Text),
        ParagraphType.Bullet    => Bullet(p.Text),
        _                       => Body(p.Text),
    };

    static Text Title(string text) => new(text)
    {
        FontSize   = 22,
        FontWeight = FontWeight.SemiBold,
        Modifier   = Modifier.Companion.Padding(horizontal: 16, vertical: 8),
    };

    static Text Caption(string text) => new(text)
    {
        FontSize = 13,
        Color    = Subtle,
        Modifier = Modifier.Companion.Padding(horizontal: 16, vertical: 4),
    };

    static Text Header(string text) => new(text)
    {
        FontSize   = 20,
        FontWeight = FontWeight.SemiBold,
        Modifier   = Modifier.Companion.Padding(start: 16, top: 16, end: 16, bottom: 4),
    };

    static Text Subhead(string text) => new(text)
    {
        FontSize   = 18,
        FontWeight = FontWeight.Medium,
        Modifier   = Modifier.Companion.Padding(start: 16, top: 12, end: 16, bottom: 4),
    };

    static Text Body(string text) => new(text)
    {
        FontSize  = 16,
        Modifier  = Modifier.Companion.Padding(horizontal: 16, vertical: 4),
    };

    static Row CodeBlock(string text) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 4),
            new Text(text)
            {
                FontSize   = 14,
                FontFamily = FontFamily.Monospace,
                Modifier   = Modifier.Companion
                    .FillMaxWidth()
                    .Clip(6)
                    .Background(CodeBg)
                    .Padding(horizontal: 12, vertical: 8),
            },
        };

    static Row Quote(string text) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(start: 16, top: 8, end: 16, bottom: 8),
            new Box
            {
                Modifier.Companion.Width(4).Height(40).Background(QuoteBar),
            },
            new Spacer(Modifier.Companion.Width(12)),
            new Text(text)
            {
                FontSize   = 16,
                FontStyle  = FontStyle.Italic,
            },
        };

    static Row Bullet(string text) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(start: 16, top: 4, end: 16, bottom: 4),
            new Text("•")
            {
                FontSize = 16,
                Modifier = Modifier.Companion.Padding(end: 8, top: 0, start: 0, bottom: 0),
            },
            new Text(text) { FontSize = 16 },
        };
}
