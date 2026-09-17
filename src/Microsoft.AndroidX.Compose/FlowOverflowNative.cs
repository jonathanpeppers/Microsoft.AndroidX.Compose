using Android.Runtime;
using System.Diagnostics.CodeAnalysis;

namespace AndroidX.Compose;

// Only the Companion field is missing from the binding; invoke all factories through bound methods.
internal static class FlowOverflowNative
{
#pragma warning disable CS0618 // Pinned Foundation 1.11.3 overflow compatibility.
    internal static readonly Foundation.Layout.FlowRowOverflow.Companion Row =
        GetCompanion<Foundation.Layout.FlowRowOverflow.Companion>("androidx.compose.foundation.layout.FlowRowOverflow");
    internal static readonly Foundation.Layout.FlowColumnOverflow.Companion Column =
        GetCompanion<Foundation.Layout.FlowColumnOverflow.Companion>("androidx.compose.foundation.layout.FlowColumnOverflow");
#pragma warning restore CS0618

    static T GetCompanion<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(string className) where T : Java.Lang.Object
    {
        using var type = Java.Lang.Class.ForName(className, true, global::Android.App.Application.Context.ClassLoader);
        using var field = type.GetField("Companion")
            ?? throw new InvalidOperationException($"{className}.Companion field is missing.");
        var peer = field.Get(null)
            ?? throw new InvalidOperationException($"{className}.Companion is null.");
        T? companion = null;
        try
        {
            companion = peer.JavaCast<T>()
                ?? throw new InvalidOperationException($"{className}.Companion has the wrong bound type.");
            return companion;
        }
        finally
        {
            if (!ReferenceEquals(peer, companion))
                peer.Dispose();
        }
    }
}
