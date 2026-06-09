using AndroidX.Compose.Material3;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Jetchat's branded Material 3 theme — C# port of upstream's
/// <c>JetchatTheme</c> + <c>JetchatLightColorScheme</c> /
/// <c>JetchatDarkColorScheme</c> in <c>theme/Themes.kt</c>.
/// </summary>
/// <remarks>
/// <para>
/// Wraps content in a <see cref="MaterialTheme"/> whose
/// <see cref="MaterialTheme.ColorScheme"/> is the Jetchat-branded
/// blue/yellow palette (light or dark variant, chosen by Compose's
/// <c>isSystemInDarkTheme()</c> at composition time). Color tokens
/// match upstream's <c>theme/Color.kt</c>.
/// </para>
/// <para>
/// Typography parity (Karla / Montserrat) is deferred — the upstream
/// <c>JetchatTypography</c> override is not yet ported. The current
/// build inherits the Material 3 baseline typography.
/// </para>
/// </remarks>
public static class JetchatTheme
{
    static readonly Color Blue10 = Color.FromHex("#000F5E");
    static readonly Color Blue20 = Color.FromHex("#001E92");
    static readonly Color Blue30 = Color.FromHex("#002ECC");
    static readonly Color Blue40 = Color.FromHex("#1546F6");
    static readonly Color Blue80 = Color.FromHex("#B8C3FF");
    static readonly Color Blue90 = Color.FromHex("#DDE1FF");

    static readonly Color DarkBlue10 = Color.FromHex("#00036B");
    static readonly Color DarkBlue20 = Color.FromHex("#000BA6");
    static readonly Color DarkBlue30 = Color.FromHex("#1026D3");
    static readonly Color DarkBlue40 = Color.FromHex("#3648EA");
    static readonly Color DarkBlue80 = Color.FromHex("#BBC2FF");
    static readonly Color DarkBlue90 = Color.FromHex("#DEE0FF");

    static readonly Color Yellow10 = Color.FromHex("#261900");
    static readonly Color Yellow20 = Color.FromHex("#402D00");
    static readonly Color Yellow30 = Color.FromHex("#5C4200");
    static readonly Color Yellow40 = Color.FromHex("#7A5900");
    static readonly Color Yellow80 = Color.FromHex("#FABD1B");
    static readonly Color Yellow90 = Color.FromHex("#FFDE9C");

    static readonly Color Red10 = Color.FromHex("#410001");
    static readonly Color Red20 = Color.FromHex("#680003");
    static readonly Color Red30 = Color.FromHex("#930006");
    static readonly Color Red40 = Color.FromHex("#BA1B1B");
    static readonly Color Red80 = Color.FromHex("#FFB4A9");
    static readonly Color Red90 = Color.FromHex("#FFDAD4");

    static readonly Color Grey10 = Color.FromHex("#191C1D");
    static readonly Color Grey20 = Color.FromHex("#2D3132");
    static readonly Color Grey80 = Color.FromHex("#C4C7C7");
    static readonly Color Grey90 = Color.FromHex("#E0E3E3");
    static readonly Color Grey95 = Color.FromHex("#EFF1F1");
    static readonly Color Grey99 = Color.FromHex("#FBFDFD");

    static readonly Color BlueGrey30 = Color.FromHex("#45464F");
    static readonly Color BlueGrey50 = Color.FromHex("#767680");
    static readonly Color BlueGrey60 = Color.FromHex("#90909A");
    static readonly Color BlueGrey80 = Color.FromHex("#C6C5D0");
    static readonly Color BlueGrey90 = Color.FromHex("#E2E1EC");

    /// <summary>
    /// Build the Jetchat-branded light <see cref="ColorScheme"/>. Slot
    /// values match upstream's <c>JetchatLightColorScheme</c>; any slot
    /// not overridden here (e.g. surfaceContainer levels, fixed-tone
    /// pairs) falls back to the M3 baseline default.
    /// </summary>
    public static ColorScheme BuildLight() => MaterialTheme.LightColorScheme(
        primary:              Blue40,
        onPrimary:            Color.White,
        primaryContainer:     Blue90,
        onPrimaryContainer:   Blue10,
        inversePrimary:       Blue80,
        secondary:            DarkBlue40,
        onSecondary:          Color.White,
        secondaryContainer:   DarkBlue90,
        onSecondaryContainer: DarkBlue10,
        tertiary:             Yellow40,
        onTertiary:           Color.White,
        tertiaryContainer:    Yellow90,
        onTertiaryContainer:  Yellow10,
        error:                Red40,
        onError:              Color.White,
        errorContainer:       Red90,
        onErrorContainer:     Red10,
        background:           Grey99,
        onBackground:         Grey10,
        surface:              Grey99,
        onSurface:            Grey10,
        inverseSurface:       Grey20,
        inverseOnSurface:     Grey95,
        surfaceVariant:       BlueGrey90,
        onSurfaceVariant:     BlueGrey30,
        outline:              BlueGrey50);

    /// <summary>
    /// Build the Jetchat-branded dark <see cref="ColorScheme"/>. Slot
    /// values match upstream's <c>JetchatDarkColorScheme</c>; any slot
    /// not overridden here falls back to the M3 baseline default.
    /// </summary>
    public static ColorScheme BuildDark() => MaterialTheme.DarkColorScheme(
        primary:              Blue80,
        onPrimary:            Blue20,
        primaryContainer:     Blue30,
        onPrimaryContainer:   Blue90,
        inversePrimary:       Blue40,
        secondary:            DarkBlue80,
        onSecondary:          DarkBlue20,
        secondaryContainer:   DarkBlue30,
        onSecondaryContainer: DarkBlue90,
        tertiary:             Yellow80,
        onTertiary:           Yellow20,
        tertiaryContainer:    Yellow30,
        onTertiaryContainer:  Yellow90,
        error:                Red80,
        onError:              Red20,
        errorContainer:       Red30,
        onErrorContainer:     Red90,
        background:           Grey10,
        onBackground:         Grey90,
        surface:              Grey10,
        onSurface:            Grey80,
        inverseSurface:       Grey90,
        inverseOnSurface:     Grey20,
        surfaceVariant:       BlueGrey30,
        onSurfaceVariant:     BlueGrey80,
        outline:              BlueGrey60);

    /// <summary>
    /// Wrap <paramref name="content"/> in a <see cref="MaterialTheme"/>
    /// configured with the Jetchat palette, choosing the light or dark
    /// <see cref="ColorScheme"/> based on the current system theme at
    /// composition time.
    /// </summary>
    public static ComposableNode Build(ComposableNode content) =>
        new Composed(c =>
        {
            bool dark = MaterialTheme.IsSystemInDarkTheme(c);
            var scheme = Compose.Remember(
                () => dark ? BuildDark() : BuildLight(),
                dark);
            var theme = new MaterialTheme
            {
                ColorScheme = scheme,
            };
            theme.Add(content);
            return theme;
        });
}
