using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.PackageSmoke;

public static class PackageConsumer
{
    [global::AndroidX.Compose.Composable]
    public static void Greeting(IComposer composer, string name)
    {
        Composables.Text(composer, $"Hello, {name}");
    }

    public static void Render(IComposer composer)
    {
        Greeting(composer, "NuGet");
    }
}
