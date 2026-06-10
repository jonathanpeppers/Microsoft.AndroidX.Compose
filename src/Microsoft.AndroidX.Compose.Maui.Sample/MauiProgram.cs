using Microsoft.AndroidX.Compose.Maui.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>MAUI app entry point. Swaps in the Compose backend.</summary>
public static class MauiProgram
{
    /// <summary>Builds the <see cref="MauiApp"/> for this sample.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseAndroidXCompose()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
