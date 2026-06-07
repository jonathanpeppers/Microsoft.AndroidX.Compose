using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Material 3 <c>MaterialTheme</c>. Uses the Android 12+ dynamic
/// light color scheme derived from the system wallpaper (Material You)
/// — gives Google's blue/teal baseline on stock emulators rather than
/// Compose's default purple.
/// </summary>
public sealed class MaterialTheme : ComposableContainer
{
    public override void Render(IComposer composer)
    {
        // Read the composition-scoped Android context rather than
        // Android.App.Application.Context, so the theme reflects any
        // override installed by an enclosing CompositionLocalProvider
        // (e.g. a contextual content-wrapper) and updates correctly on
        // activity recreations.
        var context = Locals.LocalContext.GetCurrent(composer);
        var scheme  = DynamicTonalPaletteKt.DynamicLightColorScheme(context);
        var content = ComposableLambdas.Wrap2(composer, c => RenderChildren(c));
        MaterialThemeKt.MaterialTheme(
            colorScheme: scheme,
            shapes:      null,
            typography:  null,
            content:     content,
            _composer:   composer,
            p5:          0,
            _changed:    (int)(MaterialThemeDefault.All & ~MaterialThemeDefault.ColorScheme));
    }
}
