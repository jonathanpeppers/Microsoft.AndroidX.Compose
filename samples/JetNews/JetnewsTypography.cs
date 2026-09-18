namespace AndroidX.Compose.Samples.JetNews;

/// <summary>Applies active Material typography slots to the text facades.</summary>
internal static class JetnewsTypography
{
    internal static Text WithTypography(
        this Text text,
        AndroidX.Compose.UI.Text.TextStyle style)
    {
        text.FontSize = ToSp(style.FontSize);
        text.LineHeight = ToSp(style.LineHeight);
        text.LetterSpacing = ToSp(style.LetterSpacing);
        text.FontWeight = ToFontWeight(style.FontWeight?.Weight);
        return text;
    }

    internal static AnnotatedText WithTypography(
        this AnnotatedText text,
        AndroidX.Compose.UI.Text.TextStyle style)
    {
        text.FontSize = ToSp(style.FontSize);
        text.LineHeight = ToSp(style.LineHeight);
        text.LetterSpacing = ToSp(style.LetterSpacing);
        text.FontWeight = ToFontWeight(style.FontWeight?.Weight);
        return text;
    }

    static Sp? ToSp(long packed) => packed == 0 ? null : new Sp(packed);

    static FontWeight? ToFontWeight(int? weight) => weight switch
    {
        100 => FontWeight.Thin,
        200 => FontWeight.ExtraLight,
        300 => FontWeight.Light,
        400 => FontWeight.Normal,
        500 => FontWeight.Medium,
        600 => FontWeight.SemiBold,
        700 => FontWeight.Bold,
        800 => FontWeight.ExtraBold,
        900 => FontWeight.Black,
        _ => null,
    };
}
