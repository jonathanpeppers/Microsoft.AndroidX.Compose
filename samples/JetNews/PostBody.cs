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
    public static ComposableNode BuildParagraph(
        Paragraph paragraph,
        Action<string> onOpenLink) =>
        new Composed(c =>
        {
            var typography = c.Typography();
            var scheme = c.ColorScheme();
            return paragraph.Type switch
            {
                ParagraphType.Title     => Title(paragraph, typography, scheme, onOpenLink),
                ParagraphType.Caption   => Caption(paragraph, typography, scheme, onOpenLink),
                ParagraphType.Header    => Header(paragraph, typography, scheme, onOpenLink),
                ParagraphType.Subhead   => Subhead(paragraph, typography, scheme, onOpenLink),
                ParagraphType.Text      => Body(paragraph, typography, scheme, onOpenLink),
                ParagraphType.CodeBlock => CodeBlock(paragraph, typography, scheme, onOpenLink),
                ParagraphType.Quote     => Quote(paragraph, typography, scheme, onOpenLink),
                ParagraphType.Bullet    => Bullet(paragraph, typography, scheme, onOpenLink),
                _                       => Body(paragraph, typography, scheme, onOpenLink),
            };
        });

    static ComposableNode Title(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink) =>
        Styled(
            paragraph,
            typography.HeadlineLarge,
            scheme,
            onOpenLink,
            modifier: Modifier.Padding(horizontal: 16, vertical: 8));

    static ComposableNode Caption(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink) =>
        Styled(
            paragraph,
            typography.LabelMedium,
            scheme,
            onOpenLink,
            color: Color.FromPacked(scheme.OnSurfaceVariant),
            modifier: Modifier.Padding(horizontal: 16, vertical: 4));

    static ComposableNode Header(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink) =>
        Styled(
            paragraph,
            typography.HeadlineMedium,
            scheme,
            onOpenLink,
            modifier: Modifier.Padding(start: 16, top: 16, end: 16, bottom: 4));

    static ComposableNode Subhead(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink) =>
        Styled(
            paragraph,
            typography.HeadlineSmall,
            scheme,
            onOpenLink,
            modifier: Modifier.Padding(start: 16, top: 12, end: 16, bottom: 4));

    static ComposableNode Body(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink) =>
        Styled(
            paragraph,
            typography.BodyLarge,
            scheme,
            onOpenLink,
            modifier: Modifier.Padding(horizontal: 16, vertical: 4));

    static Row CodeBlock(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink)
    {
        var codeBackground = Color.FromPacked(scheme.OnSurface).WithOpacity(.15f);
        return new Row
        {
            Modifier
                .FillMaxWidth()
                .Padding(horizontal: 16, vertical: 4),
            Styled(
                paragraph,
                typography.BodyLarge,
                scheme,
                onOpenLink,
                fontFamily: FontFamily.Monospace,
                modifier: Modifier
                    .FillMaxWidth()
                    .Clip(6)
                    .Background(codeBackground)
                    .Padding(horizontal: 12, vertical: 8)),
        };
    }

    static Row Quote(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink) =>
        new()
        {
            Modifier
                .FillMaxWidth()
                .Padding(start: 16, top: 8, end: 16, bottom: 8),
            new Box
            {
                Modifier
                    .Width(4)
                    .Height(40)
                    .Background(Color.FromPacked(scheme.OnSurface).WithOpacity(.3f)),
            },
            Spacer.Width(12),
            Styled(
                paragraph,
                typography.BodyLarge,
                scheme,
                onOpenLink,
                fontStyle: FontStyle.Italic),
        };

    static Row Bullet(
        Paragraph paragraph,
        AndroidX.Compose.Material3.Typography typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink) =>
        new()
        {
            Modifier
                .FillMaxWidth()
                .Padding(start: 16, top: 4, end: 16, bottom: 4),
            new Text("•")
            {
                Modifier = Modifier.Padding(end: 8),
            }.WithTypography(typography.BodyLarge),
            Styled(paragraph, typography.BodyLarge, scheme, onOpenLink),
        };

    static ComposableNode Styled(
        Paragraph paragraph,
        AndroidX.Compose.UI.Text.TextStyle typography,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink,
        FontStyle? fontStyle = null,
        Color? color = null,
        FontFamily? fontFamily = null,
        Modifier? modifier = null)
    {
        if (paragraph.Markups is null || paragraph.Markups.Count == 0)
        {
            var text = new Text(paragraph.Text)
            {
                FontStyle = fontStyle,
                Color = color,
                FontFamily = fontFamily,
                Modifier = modifier,
            }.WithTypography(typography);
            return text;
        }

        var annotated = new AnnotatedText(BuildAnnotated(paragraph, scheme, onOpenLink))
        {
            FontStyle = fontStyle,
            Color = color,
            FontFamily = fontFamily,
            Modifier = modifier,
        }.WithTypography(typography);
        return annotated;
    }

    static AnnotatedString BuildAnnotated(
        Paragraph paragraph,
        AndroidX.Compose.Material3.ColorScheme scheme,
        Action<string> onOpenLink)
    {
        var b = new AnnotatedStringBuilder();
        b.Append(paragraph.Text);
        var len = paragraph.Text.Length;
        var markups = paragraph.Markups
            ?? throw new InvalidOperationException("Paragraph markups were not available while building annotated text.");
        foreach (var markup in markups)
        {
            var start = Math.Clamp(markup.Start, 0, len);
            var end = Math.Clamp(markup.End, start, len);
            if (end == start)
                continue;

            var style = StyleFor(markup.Type, scheme);
            if (markup.Type == MarkupType.Link)
                b.AddLink(LinkAnnotation.Clickable(markup.Href ?? string.Empty, onOpenLink, style), start, end);
            else
                b.AddStyle(style, start, end);
        }
        return b.ToAnnotatedString();
    }

    static SpanStyle StyleFor(
        MarkupType type,
        AndroidX.Compose.Material3.ColorScheme scheme) => type switch
    {
        MarkupType.Italic => new SpanStyle { FontStyle = FontStyle.Italic },
        MarkupType.Bold => new SpanStyle { FontWeight = FontWeight.Bold },
        MarkupType.Link => new SpanStyle
        {
            Color = Color.FromPacked(scheme.Primary),
            Decoration = TextDecoration.Underline,
        },
        MarkupType.Code => new SpanStyle
        {
            FontFamily = FontFamily.Monospace,
            Background = Color.FromPacked(scheme.OnSurface).WithOpacity(.15f),
        },
        _ => new SpanStyle(),
    };
}
