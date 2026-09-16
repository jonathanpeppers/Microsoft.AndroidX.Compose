using Android.Runtime;

namespace AndroidX.Compose;

/// <summary>
/// C# wrapper around <c>androidx.compose.ui.text.font.FontFamily</c>.
/// Retains the existing facade type for compatibility with <see cref="Text"/>
/// and <see cref="TextStyle"/>. Built-in families use generated companion
/// accessors; custom families use the official bound factory.
/// </summary>
[ComposeCompanion("androidx/compose/ui/text/font/FontFamily")]
public sealed partial class FontFamily : Java.Lang.Object
{
    FontFamily(IntPtr handle, JniHandleOwnership transfer)
        : base(handle, transfer) { }

    /// <summary>Creates a family from one or more bound Compose font descriptors.</summary>
    /// <remarks>
    /// Snapshots the input array before passing it to AndroidX. Order is preserved for
    /// fallback resolution among fonts matching the requested weight and style.
    /// No input peer is disposed or owned by this method. The resulting family retains
    /// its Java fonts independently of the managed descriptors, and owns its own JNI
    /// reference. Keep the family alive while using it in text or typography; dispose
    /// it only after those consumers are finished. Do not dispose shared built-in families.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">The array is empty or contains a null font.</exception>
    /// <exception cref="ObjectDisposedException">An input font has been disposed.</exception>
    public static FontFamily FromFonts(params UI.Text.Font.IFont[] fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        UI.Text.Font.IFont[] snapshot = [.. fonts];
        if (snapshot.Length == 0)
            throw new ArgumentException("A font family requires at least one font.", nameof(fonts));
        foreach (var font in snapshot)
        {
            if (font is null)
                throw new ArgumentException("A font family cannot contain a null font.", nameof(fonts));
            ObjectDisposedException.ThrowIf(font.Handle == IntPtr.Zero, font);
        }

        // Select the list overload: no copy-back into the caller's array or list.
        var family = UI.Text.Font.FontFamilyKt.FontFamily((IList<UI.Text.Font.IFont>)snapshot)
            ?? throw new InvalidOperationException("AndroidX returned no family from FontFamily.FromFonts.");
        var value = new FontFamily(family.Handle, JniHandleOwnership.DoNotTransfer);
        GC.KeepAlive(family);
        return value;
    }

    /// <summary>
    /// <c>FontFamily.Default</c> — the platform's system font family.
    /// </summary>
    [ComposeCompanionGetter("getDefault", ReturnDescriptor = "Landroidx/compose/ui/text/font/SystemFontFamily;")]
    public static partial FontFamily Default { get; }

    /// <summary>Generic sans-serif family (<c>FontFamily.SansSerif</c>).</summary>
    [ComposeCompanionGetter("getSansSerif", ReturnDescriptor = "Landroidx/compose/ui/text/font/GenericFontFamily;")]
    public static partial FontFamily SansSerif { get; }

    /// <summary>Generic serif family (<c>FontFamily.Serif</c>).</summary>
    [ComposeCompanionGetter("getSerif", ReturnDescriptor = "Landroidx/compose/ui/text/font/GenericFontFamily;")]
    public static partial FontFamily Serif { get; }

    /// <summary>Generic monospace family (<c>FontFamily.Monospace</c>).</summary>
    [ComposeCompanionGetter("getMonospace", ReturnDescriptor = "Landroidx/compose/ui/text/font/GenericFontFamily;")]
    public static partial FontFamily Monospace { get; }

    /// <summary>Generic cursive family (<c>FontFamily.Cursive</c>).</summary>
    [ComposeCompanionGetter("getCursive", ReturnDescriptor = "Landroidx/compose/ui/text/font/GenericFontFamily;")]
    public static partial FontFamily Cursive { get; }
}
