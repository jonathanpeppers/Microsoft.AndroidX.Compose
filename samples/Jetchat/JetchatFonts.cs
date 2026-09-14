using Android.Runtime;
using AndroidX.Compose.Material3;
using BoundTextStyle = AndroidX.Compose.UI.Text.TextStyle;

namespace AndroidX.Compose.Samples.Jetchat;

/// <summary>Jetchat's bundled Karla and Montserrat fallback families, without downloaded fonts.</summary>
internal static class JetchatFonts
{
    internal static readonly FontFamily Karla = FontFamily.FromFonts(
        Font.Resource(Resource.Font.karla_regular),
        Font.Resource(Resource.Font.karla_bold, FontWeight.Bold));

    internal static readonly FontFamily Montserrat = FontFamily.FromFonts(
        Font.Resource(Resource.Font.montserrat_regular),
        Font.Resource(Resource.Font.montserrat_light, FontWeight.Light),
        Font.Resource(Resource.Font.montserrat_medium, FontWeight.Medium),
        Font.Resource(Resource.Font.montserrat_semibold, FontWeight.SemiBold));

    internal static Typography WithFonts(Typography baseline) => baseline.Copy(
        WithFont(baseline.DisplayLarge, Montserrat),
        WithFont(baseline.DisplayMedium, Montserrat),
        WithFont(baseline.DisplaySmall, Montserrat),
        WithFont(baseline.HeadlineLarge, Montserrat),
        WithFont(baseline.HeadlineMedium, Montserrat),
        WithFont(baseline.HeadlineSmall, Montserrat),
        WithFont(baseline.TitleLarge, Montserrat),
        WithFont(baseline.TitleMedium, Montserrat),
        WithFont(baseline.TitleSmall, Karla),
        WithFont(baseline.BodyLarge, Karla),
        WithFont(baseline.BodyMedium, Montserrat),
        WithFont(baseline.BodySmall, Karla),
        WithFont(baseline.LabelLarge, Montserrat),
        WithFont(baseline.LabelMedium, Montserrat),
        WithFont(baseline.LabelSmall, Montserrat));

    static BoundTextStyle WithFont(BoundTextStyle source, FontFamily family)
    {
        var boundFamily = Java.Lang.Object.GetObject<UI.Text.Font.FontFamily>(
            family.Handle, JniHandleOwnership.DoNotTransfer)
            ?? throw new InvalidOperationException("Jetchat font family binding unavailable.");
        return source.Copy(
            color: source.Color,
            fontSize: source.FontSize,
            fontWeight: source.FontWeight,
            fontStyle: source.FontStyle,
            fontSynthesis: source.FontSynthesis,
            fontFamily: boundFamily,
            fontFeatureSettings: source.FontFeatureSettings,
            letterSpacing: source.LetterSpacing,
            baselineShift: source.BaselineShift,
            textGeometricTransform: source.TextGeometricTransform,
            localeList: source.LocaleList,
            background: source.Background,
            textDecoration: source.TextDecoration,
            shadow: source.Shadow,
            drawStyle: source.DrawStyle,
            textAlign: source.GetTextAlign(),
            textDirection: source.GetTextDirection(),
            lineHeight: source.LineHeight,
            textIndent: source.TextIndent,
            platformStyle: source.PlatformStyle,
            lineHeightStyle: source.LineHeightStyle,
            lineBreak: source.GetLineBreak(),
            hyphens: source.GetHyphens(),
            textMotion: source.TextMotion);
    }
}
