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
}
