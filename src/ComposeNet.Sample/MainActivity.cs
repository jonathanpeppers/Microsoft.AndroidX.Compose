using Android.Content;
using Android.OS;
using Android.Runtime;
using Androidx.Compose.Runtime;
using Androidx.Compose.Runtime.Internal;
using Androidx.Compose.UI.Platform;
using Java.Interop;
using Kotlin.Jvm.Functions;

namespace ComposeNet.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var composeView = new ComposeView(this);
        // Wrap a Function2<Composer, Int, Unit> in a ComposableLambda the Compose runtime can drive.
        IComposableLambda lambda = ComposableLambdaKt.ComposableLambdaInstance(
            key: 0,
            tracked: false,
            block: new HelloComposable());

        composeView.SetContent(lambda);
        SetContentView(composeView);
    }
}

[Register("composenet/sample/HelloComposable")]
public sealed class HelloComposable : Java.Lang.Object, IFunction2
{
    public Java.Lang.Object? Invoke(Java.Lang.Object? p0, Java.Lang.Object? p1)
    {
        // p0 = androidx.compose.runtime.Composer
        // p1 = java.lang.Integer ($changed bitmask from the Compose compiler)
        // For now: do nothing — this proves SetContent + ComposableLambda + Function2 plumbing all link.
        // Calling Text/Button requires androidx.compose.ui.text bindings (not yet built).
        return null;
    }
}
