using Android.Runtime;
using Android.Views.InputMethods;
using AndroidX.Compose.UI.Text.Input;

namespace Microsoft.AndroidX.Compose.DeviceTests;

// Diagnostic reads of the pinned 1.11.3 native recorder, never edits or disposes its borrowed peers.
internal static class TextFieldConnectionSnapshot
{
    internal static string Read(IInputConnection connection)
    {
        var peer = (Java.Lang.Object)connection;
        string wrapper = peer.Class?.Name ?? throw new InvalidOperationException("Input connection class missing.");
        if (wrapper.StartsWith("androidx.compose.ui.text.input.NullableInputConnectionWrapperApi", StringComparison.Ordinal))
        {
            var target = Field(peer, "delegate").Get(peer);
            if (target is null)
                return $"{wrapper}: closed delegate";
            peer = target;
        }
        string name = peer.Class?.Name ?? throw new InvalidOperationException("Native recorder class missing.");
        string valueField = name switch
        {
            "androidx.compose.foundation.text.input.internal.RecordingInputConnection" => "textFieldValue",
            "androidx.compose.ui.text.input.RecordingInputConnection" => "mTextFieldValue",
            _ => throw new InvalidOperationException($"Unexpected pinned native recorder '{name}' inside '{wrapper}'."),
        };
        var value = Field(peer, valueField).Get(peer)?.JavaCast<TextFieldValue>()
            ?? throw new InvalidOperationException("Native recorder value missing.");
        bool active = Field(peer, "isActive").GetBoolean(peer);
        return $"{wrapper} -> {name} handle=0x{peer.Handle.ToInt64():x} active={active} value={value}";
    }

    static Java.Lang.Reflect.Field Field(Java.Lang.Object peer, string name)
    {
        for (var type = peer.Class; type is not null; type = type.Superclass)
        {
            var fields = type.GetDeclaredFields()
                ?? throw new InvalidOperationException("Native recorder reflection fields missing.");
            foreach (var field in fields)
            {
                if (field.Name != name)
                    continue;
                field.Accessible = true;
                return field;
            }
        }
        throw new InvalidOperationException($"Pinned native input field '{name}' missing on '{peer.Class?.Name}'.");
    }
}
