using Android.Runtime;
using AndroidX.Compose.Runtime;

namespace ComposeNet;

/// <summary>
/// Foundation <c>Image</c> composable — renders an Android drawable
/// resource via <c>painterResource(id)</c> and then forwards to the
/// stripped <c>ImageKt.Image(Painter, ...)</c> overload through a JNI
/// bridge. The Painter type is not bound at all by
/// dotnet/android-libraries (the abstract Kotlin class has mangled JVM
/// names from inline-class methods), so the Image facade only exposes
/// the drawable-resource entry point — that's what most apps want
/// anyway.
/// </summary>
public sealed class Image : ComposableNode
{
    readonly int _resourceId;
    readonly string? _contentDescription;

    /// <summary>Render an Android drawable resource (resolved via <c>painterResource</c>).</summary>
    public Image(int drawableResourceId, string? contentDescription = null)
    {
        _resourceId = drawableResourceId;
        _contentDescription = contentDescription;
    }

    internal override void Render(IComposer composer)
    {
        var modifier = BuildModifier();

        // bit 0 (painter) is marked `!` in the declarative attribute so
        // it isn't part of `All`. bit 1 (contentDescription) is always
        // cleared — we forward the caller's value, including null,
        // which Compose treats as "decorative" (no semantics node).
        int defaults = (int)ImageDefault.All & ~(int)ImageDefault.ContentDescription;
        if (modifier is not null) defaults &= ~(int)ImageDefault.Modifier;

        IntPtr painterRef = ComposeBridges.PainterResource(_resourceId, composer);
        try
        {
            ComposeBridges.Image(
                painter:            painterRef,
                contentDescription: _contentDescription,
                modifier:           modifier,
                defaults:           defaults,
                composer:           composer);
        }
        finally
        {
            JNIEnv.DeleteLocalRef(painterRef);
        }
    }
}
