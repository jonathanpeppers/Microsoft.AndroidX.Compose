using Android.Views;
using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>
/// Reads the built-in Android composition locals beyond
/// <see cref="LocalContext"/>: <see cref="LocalConfiguration"/>
/// (orientation + screen width), <see cref="LocalView"/> (haptic
/// feedback), and <see cref="LocalResources"/> (display density).
///
/// <para>The two not exercised here:
/// <see cref="LocalLifecycleOwner"/> wraps the deprecated
/// <c>androidx.compose.ui.platform.LocalLifecycleOwner</c> upstream
/// (modern Kotlin code reads it from <c>androidx.lifecycle.compose</c>);
/// <see cref="LocalColorScheme"/> is normally accessed through
/// <c>MaterialTheme.ColorScheme</c> / <c>composer.ColorScheme()</c> in
/// app code, with direct reads mostly useful for theme-aware library
/// code.</para>
/// </summary>
public static class BuiltInCompositionLocalsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "locals-builtin",
        CategoryId:  "locals-misc",
        Title:       "Built-in CompositionLocals",
        Description: "Reads LocalConfiguration (orientation + screen width), LocalView (haptic feedback), and LocalResources (display density). LocalLifecycleOwner and LocalColorScheme are described but not exercised here — see the demo summary for why.",
        Build:       _ => new Column
        {
            new Text("LocalConfiguration, LocalView, LocalResources:"),
            new ConfigurationLabel(),
            new ResourcesLabel(),
            new HapticButton(),
        });

    sealed class ConfigurationLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var cfg = LocalConfiguration.Current(composer);
            var orientation = cfg.Orientation == Android.Content.Res.Orientation.Landscape
                ? "landscape" : "portrait";
            new Text($"  LocalConfiguration: {cfg.ScreenWidthDp}dp wide, {orientation}").Render(composer);
        }
    }

    sealed class ResourcesLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var resources = LocalResources.Current(composer);
            var density = resources.DisplayMetrics?.Density ?? 0f;
            new Text($"  LocalResources: display density = {density:F2}x").Render(composer);
        }
    }

    sealed class HapticButton : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var view = LocalView.Current(composer);
            new Button(onClick: () => view.PerformHapticFeedback(FeedbackConstants.LongPress))
            {
                new Text("Tap for haptic feedback (LocalView)"),
            }.Render(composer);
        }
    }
}
