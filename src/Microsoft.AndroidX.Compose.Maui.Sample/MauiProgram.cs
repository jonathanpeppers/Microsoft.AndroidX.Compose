using Microsoft.AndroidX.Compose.Maui.Hosting;

namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>MAUI app entry point. Swaps in the Compose backend.</summary>
public static class MauiProgram
{
    /// <summary>Builds the <see cref="MauiApp"/> for this sample.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            .UseAndroidXCompose();

        return builder.Build();
    }
}
