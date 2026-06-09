namespace AndroidX.Compose.Samples.JetNews;

/// <summary>
/// Paragraph rendering for the article screen — one factory per
/// <see cref="ParagraphType"/>. Inline run styling
/// (<see cref="MarkupType.Link"/>, <see cref="MarkupType.Code"/>,
/// <see cref="MarkupType.Italic"/>, <see cref="MarkupType.Bold"/>) is
/// applied through an <see cref="AnnotatedString"/> built from the
/// paragraph's <see cref="Paragraph.Markups"/> list, mirroring the
/// upstream <c>paragraphToAnnotatedString</c> helper.
/// </summary>
internal static class PostBody
{
    static readonly Color Subtle   = Color.FromHex("#666666");
    static readonly Color QuoteBar = Color.FromHex("#BBBBBB");
    static readonly Color CodeBg   = Color.FromHex("#F1F3F5");
    static readonly Color CodeFg   = Color.FromHex("#1F2328");

    public static ComposableNode BuildParagraph(Paragraph p) => p.Type switch
    {
        ParagraphType.Title     => Title(p),
        ParagraphType.Caption   => Caption(p),
        ParagraphType.Header    => Header(p),
        ParagraphType.Subhead   => Subhead(p),
        ParagraphType.Text      => Body(p),
        ParagraphType.CodeBlock => CodeBlock(p),
        ParagraphType.Quote     => Quote(p),
        ParagraphType.Bullet    => Bullet(p),
        _                       => Body(p),
    };

    static ComposableNode Title(Paragraph p) => Styled(p,
        fontSize: 22,
        fontWeight: FontWeight.SemiBold,
        modifier:  Modifier.Companion.Padding(horizontal: 16, vertical: 8));

    static ComposableNode Caption(Paragraph p) => Styled(p,
        fontSize: 13,
        color:    Subtle,
        modifier: Modifier.Companion.Padding(horizontal: 16, vertical: 4));

    static ComposableNode Header(Paragraph p) => Styled(p,
        fontSize:   20,
        fontWeight: FontWeight.SemiBold,
        modifier:   Modifier.Companion.Padding(start: 16, top: 16, end: 16, bottom: 4));

    static ComposableNode Subhead(Paragraph p) => Styled(p,
        fontSize:   18,
        fontWeight: FontWeight.Medium,
        modifier:   Modifier.Companion.Padding(start: 16, top: 12, end: 16, bottom: 4));

    static ComposableNode Body(Paragraph p) => Styled(p,
        fontSize: 16,
        modifier: Modifier.Companion.Padding(horizontal: 16, vertical: 4));

    static Row CodeBlock(Paragraph p) =>
        new()
        {
            Modifier.Companion
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 4),
            Styled(p,
                fontSize:   14,
                fontFamily: FontFamily.Monospace,
                color:      CodeFg,
                modifier:   Modifier.Companion
                    .FillMaxWidth()
                    .Clip(6)
                    .Background(CodeBg)
                    .Padding(horizontal: 12, vertical: 8)),
        };

    static Row Quote(Paragraph p) =>
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
            Styled(p,
                fontSize:  16,
                fontStyle: FontStyle.Italic),
        };

    static Row Bullet(Paragraph p) =>
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
            Styled(p, fontSize: 16),
        };

    /// <summary>
    /// Render <paramref name="p"/> with shared paragraph-level styling.
    /// Routes to <see cref="Text"/> when no inline markups are present
    /// and to <see cref="AnnotatedText"/> otherwise; both facades share
    /// the same styling surface so the call site doesn't branch.
    /// </summary>
    static ComposableNode Styled(
        Paragraph p,
        Sp? fontSize = null,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null,
        Color? color = null,
        FontFamily? fontFamily = null,
        Modifier? modifier = null)
    {
        if (p.Markups is null || p.Markups.Count == 0)
        {
            return new Text(p.Text)
            {
                FontSize   = fontSize,
                FontWeight = fontWeight,
                FontStyle  = fontStyle,
                Color      = color,
                FontFamily = fontFamily,
                Modifier   = modifier,
            };
        }
        return new AnnotatedText(BuildAnnotated(p))
        {
            FontSize   = fontSize,
            FontWeight = fontWeight,
            FontStyle  = fontStyle,
            Color      = color,
            FontFamily = fontFamily,
            Modifier   = modifier,
        };
    }

    static AnnotatedString BuildAnnotated(Paragraph p)
    {
        var b = new AnnotatedStringBuilder();
        b.Append(p.Text);
        var len = p.Text.Length;
        foreach (var m in p.Markups!)
        {
            // Clamp to the paragraph's actual length so a malformed
            // (start, end) range can't blow up the AnnotatedString
            // builder — upstream silently truncates the same way.
            var start = Math.Clamp(m.Start, 0, len);
            var end   = Math.Clamp(m.End,   start, len);
            if (end == start)
                continue;
            b.AddStyle(StyleFor(m.Type), start, end);
        }
        return b.ToAnnotatedString();
    }

    static SpanStyle StyleFor(MarkupType type) => type switch
    {
        MarkupType.Italic => new SpanStyle { FontStyle  = FontStyle.Italic   },
        MarkupType.Bold   => new SpanStyle { FontWeight = FontWeight.Bold    },
        MarkupType.Link   => new SpanStyle { Decoration = TextDecoration.Underline },
        MarkupType.Code   => new SpanStyle
        {
            FontFamily = FontFamily.Monospace,
            // CodeFg is forced for legibility against the fixed-light
            // CodeBg, mirroring the same dark-mode workaround the
            // CodeBlock paragraph type already applies (commit b69af97).
            Color      = CodeFg,
            Background = CodeBg,
        },
        _ => new SpanStyle(),
    };
}
