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
public sealed partial class Image
{
    /// <summary>Render an Android drawable resource with no content description (decorative).</summary>
    public Image(int drawableResourceId) : this(drawableResourceId, null) { }
}
