using Android.Runtime;
using AndroidX.Compose;
using Kotlin.Jvm.Functions;

namespace Microsoft.AndroidX.Compose.DeviceTests;

// Read the actual materialized Kotlin element, not the factory's managed input.
[Register("net/compose/devicetests/PointerInputElementProbe")]
internal sealed class PointerInputElementProbe : Java.Lang.Object, IFunction2
{
    internal LongPressDragGestureBlock? Handler { get; private set; }

    public Java.Lang.Object? Invoke(Java.Lang.Object? accumulator, Java.Lang.Object? element)
    {
        var fields = element?.Class?.GetDeclaredFields()
            ?? throw new InvalidOperationException("Modifier fold did not receive an element.");
        foreach (var field in fields)
        {
            using (field)
            {
                if (field.Type?.Name != "androidx.compose.ui.input.pointer.PointerInputEventHandler")
                    continue;
                field.Accessible = true;
                Handler = field.Get(element) as LongPressDragGestureBlock
                    ?? throw new InvalidOperationException("Native pointer element lost its managed handler.");
            }
        }
        return accumulator;
    }
}
