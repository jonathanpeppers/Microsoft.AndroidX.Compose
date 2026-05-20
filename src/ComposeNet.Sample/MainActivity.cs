using Android.Content;
using Android.OS;
using Android.Runtime;
using Androidx.Compose.Foundation.Layout;
using Androidx.Compose.Runtime;
using Androidx.Compose.Runtime.Internal;
using Androidx.Compose.UI;
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
        // p1 = java.lang.Integer ($changed bitmask the compose-compiler plugin would normally pass)
        var composer = Android.Runtime.Extensions.JavaCast<IComposer>(p0!);
        int changed = ((Java.Lang.Integer)p1!).IntValue();

        // BoxKt.Box(Modifier, Composer, int) is the simplest non-hashed composable in
        // foundation.layout — it takes only a Modifier and the compose-compiler tail.
        // We pass Modifier.Companion fetched directly via JNI (the Kotlin Companion
        // class isn't bound in C# due to a naming conflict with the Modifier interface).
        BoxKt.Box(ModifierCompanion, composer, changed);
        return null;
    }

    static IModifier? s_modifier;
    static IModifier ModifierCompanion =>
        s_modifier ??= FetchModifierCompanion();

    static IModifier FetchModifierCompanion()
    {
        // androidx.compose.ui.Modifier.Companion.$$INSTANCE
        IntPtr classRef = JNIEnv.FindClass("androidx/compose/ui/Modifier$Companion");
        try
        {
            IntPtr fieldId = JNIEnv.GetStaticFieldID(classRef, "$$INSTANCE", "Landroidx/compose/ui/Modifier$Companion;");
            IntPtr instanceRef = JNIEnv.GetStaticObjectField(classRef, fieldId);
            return Java.Lang.Object.GetObject<IModifier>(instanceRef, JniHandleOwnership.TransferLocalRef)!;
        }
        finally
        {
            JNIEnv.DeleteLocalRef(classRef);
        }
    }
}
