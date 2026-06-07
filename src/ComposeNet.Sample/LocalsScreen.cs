using AndroidX.Compose.Runtime;
using ComposeNet;

namespace ComposeNet.Sample;

/// <summary>
/// Demo for the Locals tab: shows both a built-in
/// <see cref="Locals.LocalContext"/> read and a user-defined typed
/// <see cref="CompositionLocal{T}"/> override taking effect on a
/// nested subtree.
///
/// Exercises the <see cref="ComposableNode.Render(IComposer)"/>
/// public-override path — <see cref="ThemeLabel"/> is a sample-side
/// custom composable that reads <see cref="LocalPalette"/> directly
/// from the active composer, the same way a Kotlin
/// <c>@Composable</c> would read <c>LocalPalette.current</c>.
/// </summary>
public static class LocalsScreen
{
    public record Palette(string Name, long PrimaryHex);

    static readonly Palette DefaultPalette = new("Default (M3 purple)", 0xFF6750A4);
    static readonly Palette OverridePalette = new("Override (pink)",    0xFFE91E63);

    static readonly CompositionLocal<Palette> LocalPalette =
        CompositionLocal.Of(() => DefaultPalette);

    public static ComposableNode Build() => new Column
    {
        Modifier.Companion.Padding(16),

        new Text("Locals demo"),
        new Text("Built-in Locals.LocalContext (read by the custom composable below):"),
        new PackageLabel(),

        new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 12) },

        new Text("Custom CompositionLocal<Palette>:"),
        new Text("• Outer (no provider) — should show the default palette name."),
        new ThemeLabel(),

        new Text("• Inner (wrapped in CompositionLocalProvider) — should show the override."),
        new CompositionLocalProvider
        {
            LocalPalette.Provides(OverridePalette),
            new Column
            {
                Modifier.Companion.Padding(8),
                new ThemeLabel(),
                new ThemeLabel(),
            },
        },

        new Text("• Sibling after the provider — back to the default (proves scope is bounded)."),
        new ThemeLabel(),
    };

    /// <summary>
    /// Sample-side custom composable that reads the active
    /// <see cref="LocalPalette"/> value and emits a <see cref="Text"/>
    /// describing it. Direct override of
    /// <see cref="ComposableNode.Render(IComposer)"/> from a consumer
    /// assembly — only possible because <c>Render</c> is now public.
    /// </summary>
    sealed class ThemeLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var palette = LocalPalette.GetCurrent(composer);
            new Text($"  Palette: {palette.Name} (0x{palette.PrimaryHex:X8})").Render(composer);
        }
    }

    /// <summary>
    /// Reads <see cref="Locals.LocalContext"/> at composition time
    /// and shows the host app's package name — proves the built-in
    /// locals plumbing reaches user code.
    /// </summary>
    sealed class PackageLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var ctx = Locals.LocalContext.GetCurrent(composer);
            new Text($"  PackageName = {ctx.PackageName}").Render(composer);
        }
    }
}
