using Android.Runtime;
using AndroidX.Compose;
using AndroidX.Compose.UI.Text;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal static partial class TextFieldValueTestBridges
{
#pragma warning disable CS0436 // The test assembly also receives the generator's internal marker attributes.
    [ComposeBridge(Class = "androidx/compose/ui/text/TextRange",
        JvmName = "box-impl", Signature = "(J)Landroidx/compose/ui/text/TextRange;")]
    internal static partial IntPtr BoxRangeCore(long range);
#pragma warning restore CS0436

    internal static TextRange BoxRange(long range)
    {
        var local = BoxRangeCore(range);
        try
        {
            return Java.Lang.Object.GetObject<TextRange>(local, JniHandleOwnership.DoNotTransfer)
                ?? throw new InvalidOperationException("Native TextRange boxing returned null.");
        }
        finally { JNIEnv.DeleteLocalRef(local); }
    }
}
