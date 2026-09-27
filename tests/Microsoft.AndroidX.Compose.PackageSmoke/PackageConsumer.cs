using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.PackageSmoke;

public static class PackageConsumer
{
    [global::AndroidX.Compose.Composable]
    public static void Greeting(IComposer composer, string name)
    {
        Composables.Text($"Hello, {name}");
    }

    public static void Render(IComposer composer)
    {
        Greeting(composer, "NuGet");
        new Row
        {
            Modifier.Semantics(s => s.SelectableGroup().ContentDescription("Options")),
            new Text("Option"),
        }.Render(composer);
        new Slider(0.5f, _ => { })
        {
            Modifier = Modifier.ClearAndSetSemantics(s => s
                .ContentDescription("Progress")
                .Progress(0.5f, new FloatRange(0f, 1f), _ => true)),
        }.Render(composer);
    }
}
