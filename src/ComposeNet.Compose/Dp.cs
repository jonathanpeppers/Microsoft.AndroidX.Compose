namespace ComposeNet;

/// <summary>
/// C# mirror of Kotlin's <c>androidx.compose.ui.unit.Dp</c> — a
/// density-independent pixel value. Compose's <c>Dp</c> is a
/// <c>@JvmInline value class</c> wrapping a <c>Float</c>; the binder
/// strips overloads that mention it, so this struct exists purely as
/// a call-site C# representation. The bridge generator recognizes
/// <c>Dp?</c> and lowers it to the underlying <c>float</c> JNI slot
/// (see <c>ComposeNet.SourceGenerators.ComposeValueTypes</c>).
///
/// Construct via the static factories or the extension methods on
/// <see cref="DpExtensions"/>: <c>16.Dp()</c>, <c>0.5f.Dp()</c>,
/// <c>Dp.Hairline</c>, <c>Dp.Unspecified</c>.
/// </summary>
public readonly record struct Dp(float Value)
{
    /// <summary>The smallest representable Dp value (<c>Dp.Hairline</c>).</summary>
    public static Dp Hairline => new(float.Epsilon);

    /// <summary>Kotlin's <c>Dp.Unspecified</c> sentinel (<c>Float.NaN</c>).</summary>
    public static Dp Unspecified => new(float.NaN);

    /// <summary><c>0.dp</c>.</summary>
    public static Dp Zero => new(0f);

    /// <summary>Construct from an integer dp value.</summary>
    public static Dp From(int dp) => new(dp);

    /// <summary>Construct from a floating-point dp value.</summary>
    public static Dp From(float dp) => new(dp);

    /// <summary>
    /// Pack a nullable <see cref="Dp"/> into the raw <c>float</c> the
    /// JNI slot expects. <c>null</c> → <c>0f</c>, which the auto-mask
    /// in the bridge generator pairs with leaving the matching
    /// <c>$default</c> bit set so Kotlin substitutes its real default.
    /// </summary>
    public static float Pack(Dp? value) => value?.Value ?? 0f;
}

/// <summary>
/// Convenience constructors for the <see cref="ComposeNet.Dp"/>
/// struct: <c>16.Dp()</c>, <c>0.5f.Dp()</c>.
/// </summary>
public static class DpExtensions
{
    /// <summary>Wrap an integer dp value in a <c>Dp</c>.</summary>
    public static Dp Dp(this int value) => new(value);

    /// <summary>Wrap a float dp value in a <c>Dp</c>.</summary>
    public static Dp Dp(this float value) => new(value);
}
