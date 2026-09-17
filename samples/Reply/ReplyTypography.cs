namespace AndroidX.Compose.Samples.Reply;

internal static class ReplyTypography
{
    internal static readonly TextStyle HeadlineLarge = Create(32, 40, 0f, FontWeight.SemiBold);
    internal static readonly TextStyle HeadlineMedium = Create(28, 36, 0f, FontWeight.SemiBold);
    internal static readonly TextStyle HeadlineSmall = Create(24, 32, 0f, FontWeight.SemiBold);
    internal static readonly TextStyle TitleLarge = Create(22, 28, 0f, FontWeight.SemiBold);
    internal static readonly TextStyle TitleMedium = Create(16, 24, 0.15f, FontWeight.SemiBold);
    internal static readonly TextStyle TitleSmall = Create(14, 20, 0.1f, FontWeight.Bold);
    internal static readonly TextStyle BodyLarge = Create(16, 24, 0.15f, FontWeight.Normal);
    internal static readonly TextStyle BodyMedium = Create(14, 20, 0.25f, FontWeight.Medium);
    internal static readonly TextStyle BodySmall = Create(12, 16, 0.4f, FontWeight.Bold);
    internal static readonly TextStyle LabelLarge = Create(14, 20, 0.1f, FontWeight.SemiBold);
    internal static readonly TextStyle LabelMedium = Create(12, 16, 0.5f, FontWeight.SemiBold);
    internal static readonly TextStyle LabelSmall = Create(11, 16, 0.5f, FontWeight.SemiBold);

    internal static AndroidX.Compose.Material3.Typography CreateTypography() =>
        MaterialTheme.BuildTypography(
            headlineLarge: HeadlineLarge,
            headlineMedium: HeadlineMedium,
            headlineSmall: HeadlineSmall,
            titleLarge: TitleLarge,
            titleMedium: TitleMedium,
            titleSmall: TitleSmall,
            bodyLarge: BodyLarge,
            bodyMedium: BodyMedium,
            bodySmall: BodySmall,
            labelLarge: LabelLarge,
            labelMedium: LabelMedium,
            labelSmall: LabelSmall);

    internal static Text WithTypography(this Text text, TextStyle style)
    {
        text.FontSize = style.FontSize;
        text.LineHeight = style.LineHeight;
        text.LetterSpacing = style.LetterSpacing;
        text.FontWeight = style.FontWeight;
        return text;
    }

    static TextStyle Create(int size, int lineHeight, float letterSpacing, FontWeight weight) =>
        new()
        {
            FontSize = size,
            LineHeight = lineHeight,
            LetterSpacing = letterSpacing.Sp(),
            FontWeight = weight,
        };
}
