using AndroidX.Compose.Runtime;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.LocalsMisc;

/// <summary>Built-in LocalContext — read the host Android Context from a custom composable.</summary>
public static class LocalContextDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "locals-context",
        CategoryId:  "locals-misc",
        Title:       "LocalContext",
        Description: "A custom ComposableNode reads LocalContext.Current(composer) and prints the host app's package name — the built-in CompositionLocals plumbing reaches user code.",
        Build:       _ => new Column
        {
            new Text("Built-in LocalContext read from a user composable:"),
            new PackageLabel(),
        });

    sealed class PackageLabel : ComposableNode
    {
        public override void Render(IComposer composer)
        {
            var ctx = LocalContext.Current(composer);
            new Text($"  PackageName = {ctx.PackageName}").Render(composer);
        }
    }
}
