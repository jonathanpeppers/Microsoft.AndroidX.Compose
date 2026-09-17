namespace AndroidX.Compose.Samples.Reply;

internal static class ReplyTheme
{
    internal static ComposableNode Build(ComposableNode content) =>
        new Composed(c =>
        {
            bool dark = MaterialTheme.IsSystemInDarkTheme(c);
            var scheme = c.Remember(
                () => dark ? CreateDarkScheme() : CreateLightScheme(),
                key1: dark);
            var typography = c.Remember(ReplyTypography.CreateTypography);
            var shapes = c.Remember(() => MaterialTheme.BuildShapes(
                extraSmall: Shape.RoundedCorners(4),
                small: Shape.RoundedCorners(8),
                medium: Shape.RoundedCorners(16),
                large: Shape.RoundedCorners(24),
                extraLarge: Shape.RoundedCorners(32)));
            var theme = new MaterialTheme
            {
                ColorScheme = scheme,
                Typography = typography,
                Shapes = shapes,
                UseDynamicColor = false,
            };
            theme.Add(content);
            return theme;
        });

    static AndroidX.Compose.Material3.ColorScheme CreateLightScheme() =>
        MaterialTheme.LightColorScheme(
            primary: C(0xFF805610), onPrimary: C(0xFFFFFFFF),
            primaryContainer: C(0xFFFFDDB3), onPrimaryContainer: C(0xFF291800),
            inversePrimary: C(0xFFF4BD6F),
            secondary: C(0xFF6F5B40), onSecondary: C(0xFFFFFFFF),
            secondaryContainer: C(0xFFFBDEBC), onSecondaryContainer: C(0xFF271904),
            tertiary: C(0xFF51643F), onTertiary: C(0xFFFFFFFF),
            tertiaryContainer: C(0xFFD4EABB), onTertiaryContainer: C(0xFF102004),
            background: C(0xFFFFF8F4), onBackground: C(0xFF201B13),
            surface: C(0xFFFFF8F4), onSurface: C(0xFF201B13),
            surfaceVariant: C(0xFFF0E0CF), onSurfaceVariant: C(0xFF4F4539),
            inverseSurface: C(0xFF362F27), inverseOnSurface: C(0xFFFCEFE2),
            error: C(0xFFBA1A1A), onError: C(0xFFFFFFFF),
            errorContainer: C(0xFFFFDAD6), onErrorContainer: C(0xFF410002),
            outline: C(0xFF817567), outlineVariant: C(0xFFD3C4B4),
            scrim: C(0xFF000000),
            surfaceDim: C(0xFFE4D8CC), surfaceBright: C(0xFFFFF8F4),
            surfaceContainerLowest: C(0xFFFFFFFF), surfaceContainerLow: C(0xFFFFF1E5),
            surfaceContainer: C(0xFFF9ECDF), surfaceContainerHigh: C(0xFFF3E6DA),
            surfaceContainerHighest: C(0xFFEDE0D4));

    static AndroidX.Compose.Material3.ColorScheme CreateDarkScheme() =>
        MaterialTheme.DarkColorScheme(
            primary: C(0xFFF4BD6F), onPrimary: C(0xFF452B00),
            primaryContainer: C(0xFF633F00), onPrimaryContainer: C(0xFFFFDDB3),
            inversePrimary: C(0xFF805610),
            secondary: C(0xFFDDC2A1), onSecondary: C(0xFF3E2D16),
            secondaryContainer: C(0xFF56442A), onSecondaryContainer: C(0xFFFBDEBC),
            tertiary: C(0xFFB8CEA1), onTertiary: C(0xFF243515),
            tertiaryContainer: C(0xFF3A4C2A), onTertiaryContainer: C(0xFFD4EABB),
            background: C(0xFF18120B), onBackground: C(0xFFEDE0D4),
            surface: C(0xFF18120B), onSurface: C(0xFFEDE0D4),
            surfaceVariant: C(0xFF4F4539), onSurfaceVariant: C(0xFFD3C4B4),
            inverseSurface: C(0xFFEDE0D4), inverseOnSurface: C(0xFF362F27),
            error: C(0xFFFFB4AB), onError: C(0xFF690005),
            errorContainer: C(0xFF93000A), onErrorContainer: C(0xFFFFDAD6),
            outline: C(0xFF9C8F80), outlineVariant: C(0xFF4F4539),
            scrim: C(0xFF000000),
            surfaceDim: C(0xFF18120B), surfaceBright: C(0xFF3F3830),
            surfaceContainerLowest: C(0xFF120D07), surfaceContainerLow: C(0xFF201B13),
            surfaceContainer: C(0xFF251F17), surfaceContainerHigh: C(0xFF2F2921),
            surfaceContainerHighest: C(0xFF3B342B));

    static Color C(uint argb) => Color.FromArgb(argb);
}
