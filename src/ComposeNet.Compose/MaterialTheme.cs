using Android.OS;
using AndroidX.Compose.Foundation;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using BindingMaterialTheme = AndroidX.Compose.Material3.MaterialTheme;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>MaterialTheme</c> root. Supplies the active
/// <see cref="ColorScheme"/>, <see cref="Typography"/>, and
/// <see cref="Shapes"/> to every child composable.
/// </summary>
/// <remarks>
/// <para>
/// With no properties set, <c>MaterialTheme</c> picks an Android 12+
/// dynamic (Material You) light or dark color scheme depending on
/// <see cref="IsSystemInDarkTheme(IComposer)"/>, falling back to the
/// static M3 baseline palette on API levels &lt; 31.
/// </para>
/// <para>
/// To override a slice of the theme, set <see cref="ColorScheme"/>,
/// <see cref="Typography"/>, or <see cref="Shapes"/> directly — the
/// matching static factory methods build the scheme:
/// </para>
/// <code>
/// new MaterialTheme {
///     ColorScheme     = MaterialTheme.DarkColorScheme(),
///     UseDynamicColor = false,
///     new Text("Hello"),
/// }
/// </code>
/// <para>
/// Child composables can read the active theme via
/// <see cref="CurrentColorScheme(IComposer)"/> /
/// <see cref="CurrentTypography(IComposer)"/> /
/// <see cref="CurrentShapes(IComposer)"/>.
/// </para>
/// </remarks>
public sealed class MaterialTheme : ComposableContainer
{
    /// <summary>
    /// Optional explicit color scheme. When <see langword="null"/> (the
    /// default), the theme picks one based on <see cref="UseDynamicColor"/>
    /// and <see cref="Dark"/>.
    /// </summary>
    public ColorScheme? ColorScheme { get; set; }

    /// <summary>
    /// Optional explicit typography. When <see langword="null"/> (the
    /// default), the Compose-provided M3 baseline typography is used.
    /// </summary>
    public Typography? Typography { get; set; }

    /// <summary>
    /// Optional explicit shape set. When <see langword="null"/> (the
    /// default), the Compose-provided M3 baseline shapes are used.
    /// </summary>
    public Shapes? Shapes { get; set; }

    /// <summary>
    /// Force the theme into dark or light mode. When <see langword="null"/>
    /// (the default), the theme follows
    /// <see cref="IsSystemInDarkTheme(IComposer)"/>.
    /// </summary>
    public bool? Dark { get; set; }

    /// <summary>
    /// When <see langword="true"/> (the default) and running on Android
    /// 12 or later, use Material You dynamic color (derived from the
    /// system wallpaper). Has no effect when <see cref="ColorScheme"/>
    /// is set, or when running on API levels &lt; 31.
    /// </summary>
    public bool UseDynamicColor { get; set; } = true;

    public override void Render(IComposer composer)
    {
        bool dark = Dark ?? DarkThemeKt.IsSystemInDarkTheme(composer, 0);

        ColorScheme scheme = ColorScheme ?? ResolveDefaultScheme(composer, dark);

        // MaterialThemeDefault is generated from `[ComposeDefaults<MaterialThemeKt>]`
        // and contains a bit per optional Kotlin parameter (ColorScheme,
        // Shapes, Typography). Content is always provided. Start with
        // every bit set, then clear the bits for slots we actually
        // supply.
        var defaults = MaterialThemeDefault.All & ~MaterialThemeDefault.ColorScheme;
        if (Shapes is not null)     defaults &= ~MaterialThemeDefault.Shapes;
        if (Typography is not null) defaults &= ~MaterialThemeDefault.Typography;

        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        MaterialThemeKt.MaterialTheme(
            colorScheme: scheme,
            shapes:      Shapes,
            typography:  Typography,
            content:     content,
            _composer:   composer,
            p5:          0,
            _changed:    (int)defaults);
    }

    ColorScheme ResolveDefaultScheme(IComposer composer, bool dark)
    {
        if (UseDynamicColor && Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            // Read the composition-scoped Android context rather than
            // Android.App.Application.Context, so the theme reflects any
            // override installed by an enclosing CompositionLocalProvider
            // (e.g. a contextual content-wrapper) and updates correctly on
            // activity recreations.
            var ctx = Locals.LocalContext.GetCurrent(composer);
            return dark
                ? DynamicTonalPaletteKt.DynamicDarkColorScheme(ctx)
                : DynamicTonalPaletteKt.DynamicLightColorScheme(ctx);
        }
        return dark
            ? ComposeBridges.DefaultDarkColorScheme()
            : ComposeBridges.DefaultLightColorScheme();
    }

    /// <summary>
    /// Build the Material 3 baseline light <see cref="ColorScheme"/> —
    /// the same palette Kotlin's <c>lightColorScheme()</c> returns when
    /// every parameter is left at its default.
    /// </summary>
    public static ColorScheme LightColorScheme() => ComposeBridges.DefaultLightColorScheme();

    /// <summary>
    /// Build the Material 3 baseline dark <see cref="ColorScheme"/> —
    /// the same palette Kotlin's <c>darkColorScheme()</c> returns when
    /// every parameter is left at its default.
    /// </summary>
    public static ColorScheme DarkColorScheme() => ComposeBridges.DefaultDarkColorScheme();

    /// <summary>
    /// Build a dynamic (Material You) light color scheme from the
    /// supplied context, or
    /// <see cref="Android.App.Application.Context"/> when <paramref name="context"/>
    /// is <see langword="null"/>. Requires Android 12 (API 31) or later.
    /// </summary>
    public static ColorScheme DynamicLightColorScheme(Android.Content.Context? context = null) =>
        DynamicTonalPaletteKt.DynamicLightColorScheme(context ?? Android.App.Application.Context);

    /// <summary>
    /// Build a dynamic (Material You) dark color scheme from the
    /// supplied context, or
    /// <see cref="Android.App.Application.Context"/> when <paramref name="context"/>
    /// is <see langword="null"/>. Requires Android 12 (API 31) or later.
    /// </summary>
    public static ColorScheme DynamicDarkColorScheme(Android.Content.Context? context = null) =>
        DynamicTonalPaletteKt.DynamicDarkColorScheme(context ?? Android.App.Application.Context);

    /// <summary>
    /// Compose's <c>isSystemInDarkTheme()</c>: <see langword="true"/>
    /// when the device is currently in dark mode. Must be called from
    /// inside a composable's <c>Render</c> body.
    /// </summary>
    public static bool IsSystemInDarkTheme(IComposer composer) =>
        DarkThemeKt.IsSystemInDarkTheme(composer, 0);

    /// <summary>
    /// Read the active <see cref="ColorScheme"/> from the current
    /// <c>MaterialTheme</c> ancestor. Mirror of Kotlin's
    /// <c>MaterialTheme.colorScheme</c>. Must be called from inside a
    /// composable's <c>Render</c> body.
    /// </summary>
    public static ColorScheme CurrentColorScheme(IComposer composer) =>
        BindingMaterialTheme.Instance.GetColorScheme(composer, 0);

    /// <summary>
    /// Read the active <see cref="Typography"/> from the current
    /// <c>MaterialTheme</c> ancestor. Mirror of Kotlin's
    /// <c>MaterialTheme.typography</c>. Must be called from inside a
    /// composable's <c>Render</c> body.
    /// </summary>
    public static Typography CurrentTypography(IComposer composer) =>
        BindingMaterialTheme.Instance.GetTypography(composer, 0);

    /// <summary>
    /// Read the active <see cref="Shapes"/> from the current
    /// <c>MaterialTheme</c> ancestor. Mirror of Kotlin's
    /// <c>MaterialTheme.shapes</c>. Must be called from inside a
    /// composable's <c>Render</c> body.
    /// </summary>
    public static Shapes CurrentShapes(IComposer composer) =>
        BindingMaterialTheme.Instance.GetShapes(composer, 0);
}
