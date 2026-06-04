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
    internal override void Render(IComposer composer)
    {
        var scheme  = DynamicTonalPaletteKt.DynamicLightColorScheme(Android.App.Application.Context);
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
