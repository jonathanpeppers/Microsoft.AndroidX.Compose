using Android.Runtime;

namespace AndroidX.Compose;

[ComposeCompanion("androidx/compose/ui/text/input/TextFieldValue")]
internal sealed partial class TextFieldValueSaver : Java.Lang.Object
{
    TextFieldValueSaver(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

    // The binding exposes TextFieldValue, but strips its Saver-typed companion getter.
    [ComposeCompanionGetter("getSaver", ReturnDescriptor = "Landroidx/compose/runtime/saveable/Saver;")]
    public static partial TextFieldValueSaver Instance { get; }
}
