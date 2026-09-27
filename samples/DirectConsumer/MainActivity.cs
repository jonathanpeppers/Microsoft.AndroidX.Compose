using Android.Views;
using AndroidX.Activity;

namespace AndroidX.Compose.Samples.DirectConsumer;

[Activity(
    Label = "Compose Direct Consumer",
    MainLauncher = true,
    Exported = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize)]
public sealed class MainActivity : ComponentActivity
{
    static int s_instanceCount;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Android.Util.Log.Info("ComposeDirectConsumer",
            $"MainActivity.OnCreate instance={Interlocked.Increment(ref s_instanceCount)} restored={savedInstanceState is not null}");
        this.EnableEdgeToEdge();
        this.SetContent(DirectConsumerApp.Build);
    }
}
