using Microsoft.AndroidX.Compose.Maui.Handlers;
using MauiButton = Microsoft.Maui.Controls.Button;
using MauiEntry = Microsoft.Maui.Controls.Entry;
using MauiImage = Microsoft.Maui.Controls.Image;
using MauiLabel = Microsoft.Maui.Controls.Label;

namespace Microsoft.AndroidX.Compose.Maui.Hosting;

/// <summary>
/// <see cref="MauiAppBuilder"/> extensions that switch MAUI's Android
/// renderers over to Jetpack Compose by registering Compose-backed
/// replacements for the stock view handlers.
/// </summary>
public static class AppHostBuilderExtensions
{
    /// <summary>
    /// Registers the Compose-backed handlers shipped by
    /// <c>Microsoft.AndroidX.Compose.Maui</c>. Call <i>after</i>
    /// <c>UseMauiApp&lt;TApp&gt;()</c> so our handlers overwrite the stock
    /// AppCompat / Material handlers in MAUI's registry (last
    /// <c>AddHandler</c> per virtual-view type wins).
    /// </summary>
    /// <example>
    /// <code>
    /// var builder = MauiApp.CreateBuilder()
    ///     .UseMauiApp&lt;App&gt;()
    ///     .UseAndroidXCompose();
    /// </code>
    /// </example>
    public static MauiAppBuilder UseAndroidXCompose(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<MauiLabel, LabelHandler>();
            handlers.AddHandler<MauiButton, ButtonHandler>();
            handlers.AddHandler<MauiEntry, EntryHandler>();
            handlers.AddHandler<MauiImage, ImageHandler>();
        });

        return builder;
    }
}
