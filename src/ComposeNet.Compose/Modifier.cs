using Android.Runtime;
using AndroidX.Compose.UI;
using Java.Interop;

namespace ComposeNet;

/// <summary>
/// C# mirror of Kotlin's <c>androidx.compose.ui.Modifier</c> chain.
/// Build a chain by calling fluent methods on
/// <see cref="Companion"/> — each call returns a NEW immutable
/// <see cref="Modifier"/> with the op appended.
///
/// On a container (anything deriving from
/// <see cref="ComposableContainer"/>) the chain is added as a
/// collection-initializer item — C# doesn't allow mixing object- and
/// collection-initializers in the same braces, so
/// <see cref="ComposableContainer.Add(Modifier)"/> sets the
/// <see cref="ComposableNode.Modifier"/> property for you:
///
/// <code>
/// new Column
/// {
///     Modifier.Companion.Padding(16).FillMaxWidth(),
///     new Text("Hello"),
/// }
/// </code>
///
/// On a leaf composable use object-initializer syntax:
///
/// <code>
/// new Text("Hello") { Modifier = Modifier.Companion.Padding(8) }
/// </code>
///
/// At <c>Render</c> time the chain is materialized into an
/// <c>IModifier</c> by replaying each op against
/// <c>androidx.compose.ui.Modifier.Companion</c> (resolved via the
/// <c>$$INSTANCE</c> static field Kotlin emits for <c>object</c>
/// declarations) via JNI — see
/// <see cref="ComposeBridges.ModifierCompanionInstance"/>. Building
/// cheap modifier chains every recomposition is the per-composition
/// cost the Tier 1.5 facade pays — Tier 2 codegen would skip it.
///
/// Phase 1 ships <see cref="Padding(int)"/>, the horizontal/vertical
/// + per-edge overloads, and <see cref="FillMaxWidth"/> /
/// <see cref="FillMaxHeight"/> / <see cref="FillMaxSize"/>. Background,
/// border, clip, clickable, and gesture modifiers land in later
/// phases (issue #21).
/// </summary>
public sealed class Modifier
{
    static readonly System.Func<IntPtr, IntPtr>[] EmptyOps = System.Array.Empty<System.Func<IntPtr, IntPtr>>();
    static readonly Modifier _companion = new Modifier(EmptyOps);

    /// <summary>
    /// The empty Modifier — entry point for the fluent chain.
    /// Mirrors Kotlin's <c>Modifier.Companion</c> object, which is
    /// also the identity element of <c>Modifier.then(...)</c>.
    /// </summary>
    public static Modifier Companion => _companion;

    readonly System.Func<IntPtr, IntPtr>[] _ops;

    Modifier(System.Func<IntPtr, IntPtr>[] ops)
    {
        _ops = ops;
    }

    Modifier Append(System.Func<IntPtr, IntPtr> op)
    {
        var arr = new System.Func<IntPtr, IntPtr>[_ops.Length + 1];
        System.Array.Copy(_ops, arr, _ops.Length);
        arr[_ops.Length] = op;
        return new Modifier(arr);
    }

    /// <summary>
    /// Concatenate <paramref name="other"/> onto this chain, returning
    /// a new <see cref="Modifier"/>. Mirrors Kotlin's
    /// <c>Modifier.then(other)</c> — the receiver's ops apply first,
    /// then <paramref name="other"/>'s ops.
    /// </summary>
    public Modifier Then(Modifier other)
    {
        System.ArgumentNullException.ThrowIfNull(other);
        if (other._ops.Length == 0) return this;
        if (_ops.Length == 0) return other;
        var arr = new System.Func<IntPtr, IntPtr>[_ops.Length + other._ops.Length];
        System.Array.Copy(_ops, arr, _ops.Length);
        System.Array.Copy(other._ops, 0, arr, _ops.Length, other._ops.Length);
        return new Modifier(arr);
    }

    /// <summary>
    /// <c>Modifier.padding(all: Dp)</c> — applies <paramref name="allDp"/>
    /// of padding to every edge.
    /// </summary>
    public Modifier Padding(int allDp)
    {
        var dp = (float)allDp;
        return Append(h => ComposeBridges.ModifierPaddingAll(h, dp));
    }

    /// <summary>
    /// <c>Modifier.padding(horizontal: Dp, vertical: Dp)</c>.
    /// </summary>
    public Modifier Padding(int horizontalDp, int verticalDp)
    {
        var h = (float)horizontalDp;
        var v = (float)verticalDp;
        return Append(curr => ComposeBridges.ModifierPaddingHV(curr, h, v));
    }

    /// <summary>
    /// <c>Modifier.padding(start: Dp, top: Dp, end: Dp, bottom: Dp)</c>.
    /// </summary>
    public Modifier Padding(int startDp, int topDp, int endDp, int bottomDp)
    {
        var s = (float)startDp;
        var t = (float)topDp;
        var e = (float)endDp;
        var b = (float)bottomDp;
        return Append(curr => ComposeBridges.ModifierPaddingLTRB(curr, s, t, e, b));
    }

    /// <summary>
    /// <c>Modifier.fillMaxWidth(fraction)</c>. Defaults to filling
    /// the entire available width (<paramref name="fraction"/> = 1).
    /// </summary>
    public Modifier FillMaxWidth(float fraction = 1f) =>
        Append(h => ComposeBridges.ModifierFillMaxWidth(h, fraction));

    /// <summary>
    /// <c>Modifier.fillMaxHeight(fraction)</c>.
    /// </summary>
    public Modifier FillMaxHeight(float fraction = 1f) =>
        Append(h => ComposeBridges.ModifierFillMaxHeight(h, fraction));

    /// <summary>
    /// <c>Modifier.fillMaxSize(fraction)</c> — fills both width and height.
    /// </summary>
    public Modifier FillMaxSize(float fraction = 1f) =>
        Append(h => ComposeBridges.ModifierFillMaxSize(h, fraction));

    /// <summary>
    /// <c>Modifier.height(dp)</c> — sets a fixed height in dp.
    /// Required to give vertically-scrolling content (e.g.
    /// <see cref="LazyColumn{T}"/>) a bounded viewport when it lives
    /// inside an unbounded parent like a regular <see cref="Column"/>.
    /// </summary>
    public Modifier Height(int dp)
    {
        var f = (float)dp;
        return Append(h => ComposeBridges.ModifierHeight(h, f));
    }

    /// <summary>
    /// <c>Modifier.width(dp)</c> — sets a fixed width in dp.
    /// </summary>
    public Modifier Width(int dp)
    {
        var f = (float)dp;
        return Append(h => ComposeBridges.ModifierWidth(h, f));
    }

    /// <summary>
    /// <c>Modifier.size(dp)</c> — sets both width and height to the
    /// same value in dp.
    /// </summary>
    public Modifier Size(int dp)
    {
        var f = (float)dp;
        return Append(h => ComposeBridges.ModifierSizeAll(h, f));
    }

    /// <summary>
    /// <c>Modifier.size(width, height)</c> in dp.
    /// </summary>
    public Modifier Size(int widthDp, int heightDp)
    {
        var w = (float)widthDp;
        var h = (float)heightDp;
        return Append(curr => ComposeBridges.ModifierSizeWH(curr, w, h));
    }

    /// <summary>
    /// <c>Modifier.padding(paddingValues)</c> — pads using the
    /// <c>PaddingValues</c> handle a layout (e.g. <see cref="Scaffold"/>)
    /// passes to its content lambda. Internal: only Scaffold-shaped
    /// composables that receive a runtime <c>PaddingValues</c> need it.
    /// </summary>
    internal Modifier Padding(IntPtr paddingValues) =>
        Append(curr => ComposeBridges.ModifierPaddingValues(curr, paddingValues));

    /// <summary>
    /// <c>Modifier.safeDrawingPadding()</c> — pads for the union of
    /// system bars, IME, and display cutouts. Use as the outer modifier
    /// on a root composable (or inside a <see cref="Scaffold"/>'s body
    /// when no <see cref="Scaffold.TopBar"/> is supplied) to keep
    /// content out of inset regions under edge-to-edge.
    /// </summary>
    public Modifier SafeDrawingPadding() =>
        Append(h => ComposeBridges.ModifierSafeDrawingPadding(h));

    /// <summary>
    /// <c>Modifier.systemBarsPadding()</c> — pads for status + nav bars
    /// only (ignoring IME and cutouts). Prefer
    /// <see cref="SafeDrawingPadding"/> in most apps.
    /// </summary>
    public Modifier SystemBarsPadding() =>
        Append(h => ComposeBridges.ModifierSystemBarsPadding(h));

    /// <summary>
    /// Materialize the chain into a managed <c>IModifier</c> wrapper.
    /// Returns <c>null</c> when the chain is empty (no ops appended) so
    /// callers can keep the Kotlin <c>$default</c> bit set and let
    /// Compose substitute the real default.
    /// </summary>
    internal IModifier? Build()
    {
        if (_ops.Length == 0)
            return null;

        IntPtr current = ComposeBridges.ModifierCompanionInstance();
        try
        {
            for (int i = 0; i < _ops.Length; i++)
            {
                IntPtr next = _ops[i](current);
                JNIEnv.DeleteLocalRef(current);
                current = next;
            }
        }
        catch
        {
            if (current != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(current);
            throw;
        }

        return Java.Lang.Object.GetObject<IModifier>(current, JniHandleOwnership.TransferLocalRef)!;
    }
}
