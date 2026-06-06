namespace ComposeNet;

/// <summary>
/// C# mirror of Kotlin's <c>androidx.compose.ui.text.style.TextAlign</c>
/// — a <c>@JvmInline value class</c> wrapping an <c>Int</c>. The bridge
/// generator lowers <c>TextAlign?</c> to the underlying <c>int</c> JNI
/// slot.
///
/// Values mirror the Kotlin <c>TextAlign.Companion</c> constants:
/// <list type="bullet">
///   <item><see cref="Left"/> = 1</item>
///   <item><see cref="Right"/> = 2</item>
///   <item><see cref="Center"/> = 3</item>
///   <item><see cref="Justify"/> = 4</item>
///   <item><see cref="Start"/> = 5</item>
///   <item><see cref="End"/> = 6</item>
/// </list>
/// </summary>
public readonly record struct TextAlign(int Value)
{
    /// <summary>Align text to the left edge of the container.</summary>
    public static TextAlign Left => new(1);

    /// <summary>Align text to the right edge of the container.</summary>
    public static TextAlign Right => new(2);

    /// <summary>Center text within the container.</summary>
    public static TextAlign Center => new(3);

    /// <summary>Stretch lines to fill the container width.</summary>
    public static TextAlign Justify => new(4);

    /// <summary>Align text to the layout-direction start edge.</summary>
    public static TextAlign Start => new(5);

    /// <summary>Align text to the layout-direction end edge.</summary>
    public static TextAlign End => new(6);

    /// <summary>Kotlin's <c>TextAlign.Unspecified</c> sentinel.</summary>
    public static TextAlign Unspecified => new(int.MinValue);

    /// <summary>
    /// Pack a nullable <see cref="TextAlign"/> into the raw <c>int</c>
    /// the JNI slot expects. <c>null</c> → <c>0</c>; the auto-mask
    /// leaves the <c>$default</c> bit set so Kotlin's real default
    /// applies.
    /// </summary>
    public static int Pack(TextAlign? value) => value?.Value ?? 0;
}
