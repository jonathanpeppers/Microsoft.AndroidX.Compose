using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace MyApplication;

/// <summary>Defines the application's Material 3 theme.</summary>
internal static class AppTheme
{
    static readonly Color Purple80 = Color.FromHex("#FFD0BCFF");
    static readonly Color PurpleGrey80 = Color.FromHex("#FFCCC2DC");
    static readonly Color Pink80 = Color.FromHex("#FFEFB8C8");
    static readonly Color Purple40 = Color.FromHex("#FF6650A4");
    static readonly Color PurpleGrey40 = Color.FromHex("#FF625B71");
    static readonly Color Pink40 = Color.FromHex("#FF7D5260");

    /// <summary>Wraps content in the application's Material 3 theme.</summary>
    internal static MaterialTheme Build(IComposer composer, ComposableNode content)
    {
        bool dark = MaterialTheme.IsSystemInDarkTheme(composer);
        AndroidX.Compose.Material3.ColorScheme colorScheme;
        // Material You dynamic colors are available on Android 12 and later.
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var context = LocalContext.Current(composer);
            colorScheme = dark
                ? MaterialTheme.DynamicDarkColorScheme(context)
                : MaterialTheme.DynamicLightColorScheme(context);
        }
        else
        {
            colorScheme = dark
                ? MaterialTheme.DarkColorScheme(
                    primary: Purple80,
                    secondary: PurpleGrey80,
                    tertiary: Pink80)
                : MaterialTheme.LightColorScheme(
                    primary: Purple40,
                    secondary: PurpleGrey40,
                    tertiary: Pink40);
        }

        var theme = new MaterialTheme
        {
            Dark = dark,
            ColorScheme = colorScheme,
            Typography = MaterialTheme.BuildTypography(
                bodyLarge: new TextStyle
                {
                    FontFamily = FontFamily.Default,
                    FontWeight = FontWeight.Normal,
                    FontSize = new Sp(16),
                    LineHeight = new Sp(24),
                    LetterSpacing = new Sp(1) * 0.5f,
                }),
        };
        theme.Add(content);
        return theme;
    }
}
