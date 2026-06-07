using Android.Runtime;
using AndroidX.Compose.UI.Graphics.Vector;

namespace ComposeNet;

// Hand-written JNI helper for Material 3 Icons.
//
// `Xamarin.AndroidX.Compose.Material.Icons.Core(.Android)` 1.7.8.5
// ships the Java library but does NOT include any C# bindings — the
// .dll is empty. Each icon's ImageVector is exposed as a Kotlin
// extension property (e.g. `val Icons.Filled.Search: ImageVector`)
// which compiles to a static `getXxx(Icons$Variant): ImageVector`
// on a per-icon Kotlin file class. We invoke those getters via JNI
// and hand the resulting ImageVector handle to managed code as a
// bound `AndroidX.Compose.UI.Graphics.Vector.ImageVector`.
//
// The variant marker objects (`Icons.Filled`, `Icons.AutoMirrored.Filled`)
// are Kotlin singletons whose INSTANCE static field we cache once
// per variant.
internal static class IconBridge
{
    /// <summary>
    /// Cache for variant marker `INSTANCE` global refs (e.g.
    /// <c>Icons.Filled</c>). Key is the JVM class name.
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<string, IntPtr> s_variantInstances = new();

    /// <summary>
    /// Cache for resolved icon getter classes + method IDs. Key is
    /// <c>"&lt;ktClass&gt;.&lt;getter&gt;"</c>.
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<string, (IntPtr cls, IntPtr mid)> s_getters = new();

    /// <summary>
    /// Cached resolved ImageVector global refs. Same icon, same
    /// instance — the getters themselves cache the ImageVector
    /// inside the Kt class, but caching here avoids the JNI round
    /// trip after the first call.
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<string, ImageVector> s_imageVectors = new();

    static IntPtr GetVariantInstance(string variantClass)
    {
        if (s_variantInstances.TryGetValue(variantClass, out var cached))
            return cached;

        IntPtr cls = JNIEnv.FindClass(variantClass);
        IntPtr fid = JNIEnv.GetStaticFieldID(cls, "INSTANCE", "L" + variantClass + ";");
        IntPtr local = JNIEnv.GetStaticObjectField(cls, fid);
        IntPtr global = JNIEnv.NewGlobalRef(local);
        JNIEnv.DeleteLocalRef(local);
        s_variantInstances[variantClass] = global;
        return global;
    }

    /// <summary>
    /// Resolve and cache the ImageVector for one Kotlin extension
    /// property like <c>Icons.Filled.Search</c>.
    /// </summary>
    /// <param name="ktClass">
    /// JVM class containing the static getter, e.g.
    /// <c>androidx/compose/material/icons/filled/SearchKt</c>.
    /// </param>
    /// <param name="getter">
    /// Static getter name, e.g. <c>getSearch</c>.
    /// </param>
    /// <param name="variantClass">
    /// JVM variant marker class, e.g.
    /// <c>androidx/compose/material/icons/Icons$Filled</c>.
    /// </param>
    public static unsafe ImageVector Get(string ktClass, string getter, string variantClass)
    {
        string key = ktClass + "." + getter;
        if (s_imageVectors.TryGetValue(key, out var cached))
            return cached;

        if (!s_getters.TryGetValue(key, out var entry))
        {
            IntPtr cls = JNIEnv.FindClass(ktClass);
            IntPtr mid = JNIEnv.GetStaticMethodID(
                cls, getter,
                "(L" + variantClass + ";)Landroidx/compose/ui/graphics/vector/ImageVector;");
            entry = (cls, mid);
            s_getters[key] = entry;
        }

        IntPtr instance = GetVariantInstance(variantClass);
        JValue* args = stackalloc JValue[1];
        args[0] = new JValue(instance);
        IntPtr handle = JNIEnv.CallStaticObjectMethod(entry.cls, entry.mid, args);
        var vector = Java.Lang.Object.GetObject<ImageVector>(handle, JniHandleOwnership.TransferLocalRef)!;
        s_imageVectors[key] = vector;
        return vector;
    }
}
