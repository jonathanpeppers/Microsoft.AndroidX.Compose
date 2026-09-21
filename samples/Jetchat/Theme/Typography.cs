namespace AndroidX.Compose.Samples.Jetchat.Theme;

// Numeric metrics and weights from compose-samples 4c1fe7586e2fbf1c934925ef8ab64d3803361423,
// Jetchat/theme/Typography.kt.
internal static class Typography
{
    internal static readonly TextStyle DisplayLarge = Create(57, 64, 0f, FontWeight.Light);
    internal static readonly TextStyle DisplayMedium = Create(45, 52, 0f, FontWeight.Light);
    internal static readonly TextStyle DisplaySmall = Create(36, 44, 0f, FontWeight.Normal);
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

    internal static Material3.Typography CreateJetchatTypography(FontFamily karla, FontFamily montserrat) =>
        MaterialTheme.BuildTypography(
            displayLarge: WithFamily(DisplayLarge, montserrat),
            displayMedium: WithFamily(DisplayMedium, montserrat),
            displaySmall: WithFamily(DisplaySmall, montserrat),
            headlineLarge: WithFamily(HeadlineLarge, montserrat),
            headlineMedium: WithFamily(HeadlineMedium, montserrat),
            headlineSmall: WithFamily(HeadlineSmall, montserrat),
            titleLarge: WithFamily(TitleLarge, montserrat),
            titleMedium: WithFamily(TitleMedium, montserrat),
            titleSmall: WithFamily(TitleSmall, karla),
            bodyLarge: WithFamily(BodyLarge, karla),
            bodyMedium: WithFamily(BodyMedium, montserrat),
            bodySmall: WithFamily(BodySmall, karla),
            labelLarge: WithFamily(LabelLarge, montserrat),
            labelMedium: WithFamily(LabelMedium, montserrat),
            labelSmall: WithFamily(LabelSmall, montserrat));

    // Text facades have individual properties, not a TextStyle slot. Apply only metrics/weight
    // so explicit colors, resource families, and layout remain owned by the caller.
    internal static Text WithTypography(this Text text, TextStyle style)
    {
        text.FontSize = style.FontSize;
        text.LineHeight = style.LineHeight;
        text.LetterSpacing = style.LetterSpacing;
        text.FontWeight = style.FontWeight;
        return text;
    }

    internal static AnnotatedText WithTypography(this AnnotatedText text, TextStyle style)
    {
        text.FontSize = style.FontSize;
        text.LineHeight = style.LineHeight;
        text.LetterSpacing = style.LetterSpacing;
        text.FontWeight = style.FontWeight;
        return text;
    }

    static TextStyle Create(int size, int lineHeight, float letterSpacing, FontWeight weight) => new()
    {
        FontSize = size,
        LineHeight = lineHeight,
        LetterSpacing = letterSpacing.Sp(),
        FontWeight = weight,
    };

    static TextStyle WithFamily(TextStyle style, FontFamily family) => new()
    {
        FontSize = style.FontSize,
        LineHeight = style.LineHeight,
        LetterSpacing = style.LetterSpacing,
        FontWeight = style.FontWeight,
        FontFamily = family,
    };
}
