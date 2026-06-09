using global::Android.Runtime;
using global::AndroidX.Compose.Runtime;
using global::AndroidX.Compose.UI.Graphics.Painter;
using global::AndroidX.Compose.UI.Res;

namespace Microsoft.AndroidX.Compose;

/// <summary>
/// Composable accessors for Android resources — the C# mirrors of Kotlin's
/// <c>stringResource</c> / <c>colorResource</c> / <c>dimensionResource</c> /
/// <c>booleanResource</c> / <c>integerResource</c> / <c>integerArrayResource</c> /
/// <c>stringArrayResource</c> / <c>pluralStringResource</c> / <c>painterResource</c>
/// helpers from <c>androidx.compose.ui.res</c>.
/// </summary>
/// <remarks>
/// <para>
/// Every method on this class is itself <c>@Composable</c>: it must be called
/// inside a composition pass — that is, from a <see cref="ComposableNode.Render(IComposer)"/>
/// override, a <see cref="ComposableLambda2"/> / <see cref="ComposableLambda3"/> /
/// <see cref="ComposableLambda4"/> body, or any helper composable reached from one
/// of those. The active <see cref="IComposer"/> is read from
/// <see cref="ComposeContext"/> automatically, so callers don't need to thread
/// <c>composer</c> through their own signatures — exactly the ergonomics
/// <see cref="ComposeRuntime.Remember{T}(Func{T}, int, string)"/> provides.
/// </para>
/// <para>
/// Calling any of these outside a composition throws
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// The Kotlin originals are tagged <c>@ReadOnlyComposable</c>: the result is
/// looked up from <c>LocalResources.current</c> (and <c>LocalDensity.current</c>
/// for <see cref="DimensionResource"/> / <c>LocalContext.current</c> for
/// <see cref="ColorResource"/>) and re-fetched on every recomposition. Resource
/// IDs are typically Android <c>R.string.*</c> / <c>R.color.*</c> / etc. — pass
/// them straight from the generated <c>Resource.String</c> / <c>Resource.Color</c>
/// classes.
/// </para>
/// </remarks>
public static class Resources
{
    /// <summary>
    /// Load a string resource. Mirrors Kotlin's
    /// <c>stringResource(@StringRes id: Int): String</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.string.*</c> resource id.</param>
    /// <returns>The localized string for the active configuration.</returns>
    public static string StringResource(int id)
    {
        var composer = RequireComposer(nameof(StringResource));
        return StringResources_androidKt.StringResource(id, composer, _changed: 0);
    }

    /// <summary>
    /// Load a string resource with <c>String.format</c>-style arguments.
    /// Mirrors Kotlin's
    /// <c>stringResource(@StringRes id: Int, vararg formatArgs: Any): String</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.string.*</c> resource id.</param>
    /// <param name="formatArgs">
    /// Format arguments. Primitives are boxed via the matching
    /// <c>java.lang.*</c> peers, <see cref="Java.Lang.Object"/> instances are
    /// passed through unchanged, and everything else is converted via
    /// <see cref="object.ToString"/>. A <c>null</c> entry is forwarded into
    /// the Java array unchanged — Java's formatter renders it as the literal
    /// string <c>"null"</c> for <c>%s</c> placeholders.
    /// </param>
    /// <returns>The formatted localized string for the active configuration.</returns>
    public static string StringResource(int id, params object?[] formatArgs)
    {
        ArgumentNullException.ThrowIfNull(formatArgs);
        var composer = RequireComposer(nameof(StringResource));
        var (boxed, owned) = BoxArgs(formatArgs);
        try
        {
            return StringResources_androidKt.StringResource(id, boxed, composer, _changed: 0);
        }
        finally
        {
            DisposeOwned(boxed, owned);
        }
    }

    /// <summary>
    /// Load a string-array resource. Mirrors Kotlin's
    /// <c>stringArrayResource(@ArrayRes id: Int): Array&lt;String&gt;</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.array.*</c> resource id.</param>
    public static string[] StringArrayResource(int id)
    {
        var composer = RequireComposer(nameof(StringArrayResource));
        return StringResources_androidKt.StringArrayResource(id, composer, _changed: 0);
    }

    /// <summary>
    /// Load a plurals resource. Mirrors Kotlin's
    /// <c>pluralStringResource(@PluralsRes id: Int, count: Int): String</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.plurals.*</c> resource id.</param>
    /// <param name="count">The quantity used to select the plural form.</param>
    public static string PluralStringResource(int id, int count)
    {
        var composer = RequireComposer(nameof(PluralStringResource));
        return StringResources_androidKt.PluralStringResource(id, count, composer, _changed: 0);
    }

    /// <summary>
    /// Load a plurals resource with <c>String.format</c>-style arguments. Mirrors
    /// Kotlin's <c>pluralStringResource(@PluralsRes id: Int, count: Int, vararg formatArgs: Any): String</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.plurals.*</c> resource id.</param>
    /// <param name="count">The quantity used to select the plural form.</param>
    /// <param name="formatArgs">
    /// Format arguments, boxed to <see cref="Java.Lang.Object"/> peers before
    /// crossing JNI (see <see cref="StringResource(int, object?[])"/> for the
    /// boxing rules). Pass <paramref name="count"/> again here if it appears
    /// in the format string — Kotlin's API does the same.
    /// </param>
    public static string PluralStringResource(int id, int count, params object?[] formatArgs)
    {
        ArgumentNullException.ThrowIfNull(formatArgs);
        var composer = RequireComposer(nameof(PluralStringResource));
        var (boxed, owned) = BoxArgs(formatArgs);
        try
        {
            return StringResources_androidKt.PluralStringResource(id, count, boxed, composer, _changed: 0);
        }
        finally
        {
            DisposeOwned(boxed, owned);
        }
    }

    /// <summary>
    /// Load an integer resource. Mirrors Kotlin's
    /// <c>integerResource(@IntegerRes id: Int): Int</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.integer.*</c> resource id.</param>
    public static int IntegerResource(int id)
    {
        var composer = RequireComposer(nameof(IntegerResource));
        return PrimitiveResources_androidKt.IntegerResource(id, composer, _changed: 0);
    }

    /// <summary>
    /// Load an integer-array resource. Mirrors Kotlin's
    /// <c>integerArrayResource(@ArrayRes id: Int): IntArray</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.array.*</c> resource id.</param>
    public static int[] IntegerArrayResource(int id)
    {
        var composer = RequireComposer(nameof(IntegerArrayResource));
        return PrimitiveResources_androidKt.IntegerArrayResource(id, composer, _changed: 0);
    }

    /// <summary>
    /// Load a boolean resource. Mirrors Kotlin's
    /// <c>booleanResource(@BoolRes id: Int): Boolean</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.bool.*</c> resource id.</param>
    public static bool BooleanResource(int id)
    {
        var composer = RequireComposer(nameof(BooleanResource));
        return PrimitiveResources_androidKt.BooleanResource(id, composer, _changed: 0);
    }

    /// <summary>
    /// Load a dimension resource as a <see cref="Dp"/>. Mirrors Kotlin's
    /// <c>dimensionResource(@DimenRes id: Int): Dp</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.dimen.*</c> resource id.</param>
    /// <remarks>
    /// The binding returns the raw <c>Dp</c> float (Kotlin's <c>Dp</c> is a
    /// <c>@JvmInline value class</c> around a <c>Float</c>); this wrapper boxes
    /// it back into the C# <see cref="Dp"/> struct so call sites stay typed.
    /// </remarks>
    public static Dp DimensionResource(int id)
    {
        var composer = RequireComposer(nameof(DimensionResource));
        float raw = PrimitiveResources_androidKt.DimensionResource(id, composer, _changed: 0);
        return new Dp(raw);
    }

    /// <summary>
    /// Load a color resource as a packed Compose <c>Color</c> long. Mirrors
    /// Kotlin's <c>colorResource(@ColorRes id: Int): Color</c>.
    /// </summary>
    /// <param name="id">An Android <c>R.color.*</c> resource id.</param>
    /// <remarks>
    /// Compose's <c>Color</c> is a <c>@JvmInline value class</c> around a
    /// <c>ULong</c>; the wire representation is a <see cref="long"/>. Pass the
    /// returned value directly to any Microsoft.AndroidX.Compose API that takes a color
    /// (<c>Surface.ContainerColor</c>, <c>Modifier.Background(long)</c>,
    /// <c>Icon.TintArgb</c>, …).
    /// </remarks>
    public static long ColorResource(int id)
    {
        var composer = RequireComposer(nameof(ColorResource));
        return ColorResources_androidKt.ColorResource(id, composer, _changed: 0);
    }

    /// <summary>
    /// Load a drawable resource as a <see cref="Painter"/>. Mirrors Kotlin's
    /// <c>painterResource(@DrawableRes id: Int): Painter</c>.
    /// </summary>
    /// <param name="id">
    /// An Android <c>R.drawable.*</c> resource id. Supports both bitmap drawables
    /// (PNG, WebP, …) and <c>VectorDrawable</c> XML.
    /// </param>
    /// <remarks>
    /// <para>
    /// Routed through <see cref="ComposeBridges"/> because the underlying Kotlin
    /// helper is bound but its return slot is stripped — Compose's <c>Painter</c>
    /// returns are wrapped in mangled JNI signatures the binder doesn't surface.
    /// The bridge returns a fresh local <c>Painter</c> JNI ref each call; this
    /// wrapper transfers ownership of that ref to the returned peer.
    /// </para>
    /// <para>
    /// For <c>Image(int)</c> / <c>Icon(int, …)</c> use sites you typically
    /// don't need to call this directly — the facades resolve the painter
    /// internally. Use this when you need to hand the same painter to a
    /// custom drawing path or pass it across helper composables.
    /// </para>
    /// </remarks>
    public static Painter PainterResource(int id)
    {
        var composer = RequireComposer(nameof(PainterResource));
        IntPtr handle = ComposeBridges.PainterResource(id, composer);
        return Java.Lang.Object.GetObject<Painter>(handle, JniHandleOwnership.TransferLocalRef)
            ?? throw new InvalidOperationException(
                $"painterResource({id}) returned a null Painter handle.");
    }

    static IComposer RequireComposer(string memberName)
        => ComposeContext.Current
            ?? throw new InvalidOperationException(
                $"Resources.{memberName} must be called inside a composition (e.g. inside a SetContent body or a ComposableNode.Render override).");

    // Convert a C# format-args array into a Java.Lang.Object[] suitable for
    // crossing JNI. Returns a parallel bool[] flagging which entries we
    // allocated ourselves (so DisposeOwned can release them after the call)
    // versus which are caller-owned peers that must not be disposed. Caller-
    // supplied nulls propagate through into the Java array — Java's formatter
    // renders them as the literal string "null" for %s placeholders, matching
    // what a Kotlin call site with a force-cast null would produce.
    static (Java.Lang.Object[] boxed, bool[] owned) BoxArgs(object?[] args)
    {
        var boxed = new Java.Lang.Object[args.Length];
        var owned = new bool[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case null:
                    boxed[i] = null!;
                    owned[i] = false;
                    break;
                case Java.Lang.Object o:
                    boxed[i] = o;
                    owned[i] = false;
                    break;
                case string s:           boxed[i] = new Java.Lang.String(s);          owned[i] = true; break;
                case bool b:             boxed[i] = Java.Lang.Boolean.ValueOf(b)!;     owned[i] = true; break;
                case char c:             boxed[i] = Java.Lang.Character.ValueOf(c)!;   owned[i] = true; break;
                case sbyte sb:           boxed[i] = Java.Lang.Byte.ValueOf(sb)!;       owned[i] = true; break;
                case byte by:            boxed[i] = Java.Lang.Short.ValueOf((short)by)!; owned[i] = true; break;
                case short sh:           boxed[i] = Java.Lang.Short.ValueOf(sh)!;      owned[i] = true; break;
                case ushort us:          boxed[i] = Java.Lang.Integer.ValueOf(us)!;    owned[i] = true; break;
                case int ii:             boxed[i] = Java.Lang.Integer.ValueOf(ii)!;    owned[i] = true; break;
                case uint ui:            boxed[i] = Java.Lang.Long.ValueOf(ui)!;       owned[i] = true; break;
                case long l:             boxed[i] = Java.Lang.Long.ValueOf(l)!;        owned[i] = true; break;
                case ulong ul:           boxed[i] = Java.Lang.Long.ValueOf(unchecked((long)ul))!; owned[i] = true; break;
                case float f:            boxed[i] = Java.Lang.Float.ValueOf(f)!;       owned[i] = true; break;
                case double d:           boxed[i] = Java.Lang.Double.ValueOf(d)!;      owned[i] = true; break;
                // Format-args land — unlike Compose's effect keys (which need
                // Object.equals stability), anything stringifiable is fine.
                // Fall through to ToString() so enums, decimals, DateTime,
                // records, etc. render naturally with %s.
                default:                 boxed[i] = new Java.Lang.String(args[i]!.ToString() ?? string.Empty); owned[i] = true; break;
            }
        }
        return (boxed, owned);
    }

    // Dispose only the boxes BoxArgs allocated; leave caller-supplied
    // Java.Lang.Object peers and null slots alone.
    static void DisposeOwned(Java.Lang.Object[] boxed, bool[] owned)
    {
        for (int i = 0; i < boxed.Length; i++)
            if (owned[i] && boxed[i] is not null)
                boxed[i].Dispose();
    }
}
