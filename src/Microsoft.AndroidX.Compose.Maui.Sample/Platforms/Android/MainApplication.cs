using Android.App;
using Android.Runtime;

namespace Microsoft.AndroidX.Compose.Maui.Sample;

/// <summary>Android <see cref="Android.App.Application"/> hosting the MAUI runtime.</summary>
[Application]
public class MainApplication : MauiApplication
{
    /// <inheritdoc/>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <inheritdoc/>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
