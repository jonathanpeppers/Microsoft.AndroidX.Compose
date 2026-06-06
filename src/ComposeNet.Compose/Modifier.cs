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
/// Phase 1 ships <see cref="Padding(ComposeNet.Dp)"/>, the horizontal/vertical
/// + per-edge overloads, and <see cref="FillMaxWidth"/> /
/// <see cref="FillMaxHeight"/> / <see cref="FillMaxSize"/>. Phase 2
/// adds <see cref="Background(long)"/>, <see cref="Border(ComposeNet.Dp, long)"/>,
/// <see cref="Clip(ComposeNet.Dp)"/>, and <see cref="Clickable"/>. Gesture and
/// size-constraint modifiers land in later phases (issue #21).
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
    /// <c>Modifier.padding(all: Dp)</c> — applies <paramref name="all"/>
    /// of padding to every edge.
    /// </summary>
    public Modifier Padding(Dp all)
    {
        var dp = all.Value;
        return Append(h => ComposeBridges.ModifierPaddingAll(h, dp));
    }

    /// <summary>
    /// <c>Modifier.padding(horizontal: Dp, vertical: Dp)</c>.
    /// </summary>
    public Modifier Padding(Dp horizontal, Dp vertical)
    {
        var h = horizontal.Value;
        var v = vertical.Value;
        return Append(curr => ComposeBridges.ModifierPaddingHV(curr, h, v));
    }

    /// <summary>
    /// <c>Modifier.padding(start: Dp, top: Dp, end: Dp, bottom: Dp)</c>.
    /// </summary>
    public Modifier Padding(Dp start, Dp top, Dp end, Dp bottom)
    {
        var s = start.Value;
        var t = top.Value;
        var e = end.Value;
        var b = bottom.Value;
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
    public Modifier Height(Dp height)
    {
        var f = height.Value;
        return Append(h => ComposeBridges.ModifierHeight(h, f));
    }

    /// <summary>
    /// <c>Modifier.width(dp)</c> — sets a fixed width in dp.
    /// </summary>
    public Modifier Width(Dp width)
    {
        var f = width.Value;
        return Append(h => ComposeBridges.ModifierWidth(h, f));
    }

    /// <summary>
    /// <c>Modifier.size(dp)</c> — sets both width and height to the
    /// same value in dp.
    /// </summary>
    public Modifier Size(Dp size)
    {
        var f = size.Value;
        return Append(h => ComposeBridges.ModifierSizeAll(h, f));
    }

    /// <summary>
    /// <c>Modifier.size(width, height)</c> in dp.
    /// </summary>
    public Modifier Size(Dp width, Dp height)
    {
        var w = width.Value;
        var h = height.Value;
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
    /// <c>Modifier.background(color)</c> — paints a flat fill behind the
    /// composable using a <c>RectangleShape</c>. Takes a packed Compose
    /// <c>androidx.compose.ui.graphics.Color</c> value (a <c>ULong</c>
    /// surfaced as a <c>long</c> in the binding because Color is a Kotlin
    /// <c>@JvmInline value class</c>). Build one with
    /// <see cref="AndroidX.Compose.UI.Graphics.ColorKt.Color(int, int, int, int)"/>
    /// from per-channel bytes (recommended), or
    /// <see cref="AndroidX.Compose.UI.Graphics.ColorKt.Color(int)"/> from an
    /// <c>0xAARRGGBB</c> int — note that opaque-alpha hex literals like
    /// <c>0xFF1976D2</c> are <c>uint</c> in C#, so they need an
    /// <c>unchecked((int)0xFF1976D2)</c> cast to compile against the
    /// <c>int</c> overload.
    /// </summary>
    public Modifier Background(long color) =>
        Append(curr => ComposeBridges.ModifierBackground(curr, color, null));

    /// <summary>
    /// <c>Modifier.background(color, shape)</c> — paints a flat fill
    /// behind the composable, clipped to <paramref name="shape"/>. Pass
    /// <c>null</c> for the default <c>RectangleShape</c>; otherwise build
    /// a shape via <see cref="ComposeNet.Shape.RoundedCorners(Dp)"/>,
    /// <see cref="ComposeNet.Shape.Circle"/>,
    /// <see cref="ComposeNet.Shape.CutCorners(Dp)"/>, etc. The shape is
    /// captured by the closure so its Java peer stays alive across
    /// recompositions.
    /// </summary>
    public Modifier Background(long color, Shape? shape) =>
        Append(curr => ComposeBridges.ModifierBackground(curr, color, shape?.Handle));

    /// <summary>
    /// <c>Modifier.border(width, color)</c> — draws a rectangular stroke
    /// around the composable. <paramref name="width"/> is the stroke
    /// width in density-independent pixels. <paramref name="color"/> is a
    /// packed Compose <c>Color</c> long (see
    /// <see cref="Background(long)"/> for how to build one). For rounded
    /// corners use <see cref="Border(Dp, long, Dp)"/>; for arbitrary
    /// shapes use <see cref="Border(Dp, long, Shape?)"/>.
    /// </summary>
    public Modifier Border(Dp width, long color)
    {
        var w = width.Value;
        return Append(curr => ComposeBridges.ModifierBorder(curr, w, color, null));
    }

    /// <summary>
    /// <c>Modifier.border(width, color, RoundedCornerShape(cornerRadius))</c> —
    /// draws a stroke with rounded corners. Match
    /// <paramref name="cornerRadius"/> to a <see cref="Clip(Dp)"/>
    /// earlier in the chain so the corners align (otherwise the
    /// rectangular stroke gets sliced by a rounded clip and you see
    /// jagged corner stubs).
    /// </summary>
    public Modifier Border(Dp width, long color, Dp cornerRadius)
    {
        var w = width.Value;
        var r = cornerRadius.Value;
        if (r <= 0f)
            return Append(curr => ComposeBridges.ModifierBorder(curr, w, color, null));

        return Append(curr =>
        {
            IntPtr shape = ComposeBridges.RoundedCornerShape(r);
            try
            {
                return ComposeBridges.ModifierBorder(curr, w, color, shape);
            }
            finally
            {
                if (shape != IntPtr.Zero)
                    JNIEnv.DeleteLocalRef(shape);
            }
        });
    }

    /// <summary>
    /// <c>Modifier.border(width, color, shape)</c> — overload taking an
    /// explicit <see cref="ComposeNet.Shape"/> for non-rounded geometry
    /// (cut corners, custom shape factories, etc.). Pass <c>null</c> for
    /// <c>RectangleShape</c>. The shape is captured by the closure so its
    /// Java peer stays alive across recompositions.
    /// </summary>
    public Modifier Border(Dp width, long color, Shape? shape)
    {
        var w = width.Value;
        return Append(curr => ComposeBridges.ModifierBorder(curr, w, color, shape?.Handle));
    }

    /// <summary>
    /// <c>Modifier.clip(RoundedCornerShape(<paramref name="cornerRadius"/>))</c> —
    /// rounds the four corners by the same radius and clips drawing to the
    /// resulting shape. Pass <c>0</c> for no rounding (rectangle clip).
    /// </summary>
    public Modifier Clip(Dp cornerRadius)
    {
        var dp = cornerRadius.Value;
        return Append(curr => ComposeBridges.ModifierClipRoundedCorners(curr, dp));
    }

    /// <summary>
    /// <c>Modifier.clip(shape)</c> — clips drawing (and pointer hits)
    /// to the supplied <paramref name="shape"/>. Use with
    /// <see cref="ComposeNet.Shape.Circle"/> for circular clips,
    /// <see cref="ComposeNet.Shape.CutCorners(Dp)"/> for chamfered
    /// corners, or any other shape factory. The shape is captured by
    /// the closure so its Java peer stays alive across recompositions.
    /// </summary>
    public Modifier Clip(Shape shape)
    {
        System.ArgumentNullException.ThrowIfNull(shape);
        return Append(curr => ComposeBridges.ModifierClip(curr, shape.Handle));
    }

    /// <summary>
    /// <c>Modifier.clickable { onClick() }</c> — handles taps with the
    /// default Material indication / ripple. The click handler runs on
    /// the UI thread.
    /// </summary>
    public Modifier Clickable(System.Action onClick)
    {
        if (onClick is null)
            throw new System.ArgumentNullException(nameof(onClick));

        var lambda = new ComposableLambda0(onClick);
        return Append(curr => ComposeBridges.ModifierClickable(curr, lambda));
    }

    /// <summary>
    /// <c>Modifier.verticalScroll(state)</c> — makes a non-Lazy parent
    /// (e.g. a regular <see cref="Column"/> or <see cref="Box"/>)
    /// vertically scrollable when its content overflows. Hold the
    /// <paramref name="state"/> across recompositions with
    /// <see cref="ComposeActivity.Remember{T}"/>:
    /// <code>
    /// var scroll = Remember(() =&gt; new ScrollState());
    /// new Column { Modifier.Companion.VerticalScroll(scroll), /* children */ };
    /// </code>
    /// Prefer <see cref="LazyColumn{T}"/> for long lists of like-shaped
    /// items — it only composes the children currently on screen. Use
    /// <see cref="VerticalScroll"/> for a small, known set of children
    /// that simply might overflow on smaller screens.
    /// </summary>
    /// <param name="state">Scroll position state to drive (and read
    /// the current offset from).</param>
    /// <param name="enabled">When <c>false</c>, the user can no longer
    /// scroll the content via touch gestures. <see cref="ScrollState"/>
    /// still updates as the layout changes, but this binding does not
    /// yet expose programmatic scrolling (Kotlin's
    /// <c>scrollTo</c> / <c>animateScrollTo</c> are <c>suspend</c>
    /// functions, not yet wired up).</param>
    /// <param name="reverseScrolling">When <c>true</c>, flip the
    /// scroll direction so the start of the content sits at the
    /// bottom of the viewport (and dragging up reveals earlier
    /// content). Defaults to <c>false</c>.</param>
    public Modifier VerticalScroll(ScrollState state, bool enabled = true, bool reverseScrolling = false)
    {
        System.ArgumentNullException.ThrowIfNull(state);
        var jvm = state.Jvm;
        return Append(curr =>
            ComposeBridges.ModifierVerticalScroll(
                curr, ((Java.Lang.Object)jvm).Handle, enabled, reverseScrolling));
    }

    /// <summary>
    /// <c>Modifier.horizontalScroll(state)</c> — makes a non-Lazy
    /// parent (e.g. a regular <see cref="Row"/> or <see cref="Box"/>)
    /// horizontally scrollable when its content overflows. See
    /// <see cref="VerticalScroll"/> for the typical usage shape; prefer
    /// <see cref="LazyRow{T}"/> for long horizontally-scrollable lists.
    /// </summary>
    /// <param name="state">Scroll position state to drive (and read
    /// the current offset from).</param>
    /// <param name="enabled">When <c>false</c>, the user can no longer
    /// scroll the content via touch gestures. <see cref="ScrollState"/>
    /// still updates as the layout changes, but this binding does not
    /// yet expose programmatic scrolling (Kotlin's
    /// <c>scrollTo</c> / <c>animateScrollTo</c> are <c>suspend</c>
    /// functions, not yet wired up).</param>
    /// <param name="reverseScrolling">When <c>true</c>, flip the
    /// scroll direction so the start of the content sits at the end
    /// of the viewport. Defaults to <c>false</c>.</param>
    public Modifier HorizontalScroll(ScrollState state, bool enabled = true, bool reverseScrolling = false)
    {
        System.ArgumentNullException.ThrowIfNull(state);
        var jvm = state.Jvm;
        return Append(curr =>
            ComposeBridges.ModifierHorizontalScroll(
                curr, ((Java.Lang.Object)jvm).Handle, enabled, reverseScrolling));
    }

    /// <summary>
    /// <c>Modifier.weight(weight, fill = true)</c> — only valid inside a
    /// <see cref="Row"/> or <see cref="Column"/> (or any container that
    /// publishes <see cref="ScopeKind.Row"/> / <see cref="ScopeKind.Column"/>).
    /// Distributes the leftover space along the parent's main axis in
    /// proportion to other weighted children. Set <paramref name="fill"/>
    /// to <c>false</c> to let the child be smaller than its allotted slot.
    /// </summary>
    public Modifier Weight(float weight, bool fill = true)
    {
        return Append(curr =>
        {
            IntPtr scope = RenderContext.CurrentScope;
            ScopeKind kind = RenderContext.CurrentScopeKind;
            return kind switch
            {
                ScopeKind.Row    => ComposeBridges.RowScopeModifierWeight(scope, curr, weight, fill),
                ScopeKind.Column => ComposeBridges.ColumnScopeModifierWeight(scope, curr, weight, fill),
                _ => throw new System.InvalidOperationException(
                    "Modifier.Weight() can only be used inside a Row, Column, or " +
                    "Row/Column-shaped scope (BottomAppBar, NavigationBar, …). " +
                    $"Current scope kind: {kind}.")
            };
        });
    }

    /// <summary>
    /// <c>Modifier.widthIn(min, max)</c> — adds a min and/or max width
    /// constraint. Pass <c>null</c> for either bound to leave it
    /// unconstrained (Kotlin's <c>Dp.Unspecified</c> default), so
    /// <c>WidthIn(min: 100)</c> caps only the lower bound.
    /// </summary>
    public Modifier WidthIn(Dp? min = null, Dp? max = null) =>
        Append(curr => ComposeBridges.ModifierWidthIn(curr, min, max));

    /// <summary>
    /// <c>Modifier.heightIn(min, max)</c> — adds a min and/or max height
    /// constraint. Pass <c>null</c> for either bound to leave it
    /// unconstrained.
    /// </summary>
    public Modifier HeightIn(Dp? min = null, Dp? max = null) =>
        Append(curr => ComposeBridges.ModifierHeightIn(curr, min, max));

    /// <summary>
    /// <c>Modifier.sizeIn(minWidth, minHeight, maxWidth, maxHeight)</c> —
    /// constrains both axes. Pass <c>null</c> for any bound to leave it
    /// unconstrained.
    /// </summary>
    public Modifier SizeIn(Dp? minWidth = null, Dp? minHeight = null, Dp? maxWidth = null, Dp? maxHeight = null) =>
        Append(curr => ComposeBridges.ModifierSizeIn(curr, minWidth, minHeight, maxWidth, maxHeight));

    /// <summary>
    /// <c>Modifier.requiredSize(size)</c> — declares an exact size that
    /// bypasses the parent's constraints (the composable is allowed to
    /// be drawn outside the parent if needed). Use sparingly; prefer
    /// <see cref="Size(Dp)"/> for normal layout.
    /// </summary>
    public Modifier RequiredSize(Dp size)
    {
        var dp = size.Value;
        return Append(curr => ComposeBridges.ModifierRequiredSizeAll(curr, dp));
    }

    /// <summary>
    /// <c>Modifier.requiredSize(width, height)</c> — overload taking
    /// independent width and height (each bypassing parent constraints).
    /// </summary>
    public Modifier RequiredSize(Dp width, Dp height)
    {
        var w = width.Value;
        var h = height.Value;
        return Append(curr => ComposeBridges.ModifierRequiredSizeWH(curr, w, h));
    }

    /// <summary>
    /// <c>Modifier.requiredWidth(width)</c> — declares an exact width
    /// that bypasses the parent's constraints.
    /// </summary>
    public Modifier RequiredWidth(Dp width)
    {
        var w = width.Value;
        return Append(curr => ComposeBridges.ModifierRequiredWidth(curr, w));
    }

    /// <summary>
    /// <c>Modifier.requiredHeight(height)</c> — declares an exact height
    /// that bypasses the parent's constraints.
    /// </summary>
    public Modifier RequiredHeight(Dp height)
    {
        var h = height.Value;
        return Append(curr => ComposeBridges.ModifierRequiredHeight(curr, h));
    }

    /// <summary>
    /// <c>Modifier.defaultMinSize(minWidth, minHeight)</c> — supplies a
    /// minimum size that's only used when the parent doesn't already
    /// constrain that dimension. Useful for default-sized buttons /
    /// chips that should grow with content but never shrink below a
    /// hit-target floor. Pass <c>null</c> for either bound to leave it
    /// unspecified.
    /// </summary>
    public Modifier DefaultMinSize(Dp? minWidth = null, Dp? minHeight = null) =>
        Append(curr => ComposeBridges.ModifierDefaultMinSize(curr, minWidth, minHeight));

    /// <summary>
    /// <c>Modifier.wrapContentSize(unbounded)</c> — measures content
    /// without imposing the parent's constraints, then centers the
    /// result inside the parent's available space. Set
    /// <paramref name="unbounded"/> to <c>true</c> to let content
    /// overflow the parent.
    /// </summary>
    public Modifier WrapContentSize(bool unbounded = false) =>
        Append(curr => ComposeBridges.ModifierWrapContentSize(curr, unbounded));

    /// <summary>
    /// <c>Modifier.wrapContentWidth(unbounded)</c> — same as
    /// <see cref="WrapContentSize(bool)"/> but only relaxes the width
    /// axis.
    /// </summary>
    public Modifier WrapContentWidth(bool unbounded = false) =>
        Append(curr => ComposeBridges.ModifierWrapContentWidth(curr, unbounded));

    /// <summary>
    /// <c>Modifier.wrapContentHeight(unbounded)</c> — same as
    /// <see cref="WrapContentSize(bool)"/> but only relaxes the height
    /// axis.
    /// </summary>
    public Modifier WrapContentHeight(bool unbounded = false) =>
        Append(curr => ComposeBridges.ModifierWrapContentHeight(curr, unbounded));

    /// <summary>
    /// <c>Modifier.aspectRatio(ratio, matchHeightConstraintsFirst)</c> —
    /// forces the composable's width-to-height ratio.
    /// <paramref name="ratio"/> is <c>width / height</c>: <c>16f / 9f</c>
    /// for a wide video frame, <c>1f</c> for a square. When
    /// <paramref name="matchHeightConstraintsFirst"/> is <c>true</c>, the
    /// height constraint is preferred when both width and height are
    /// bounded; otherwise width wins (Kotlin's default).
    /// </summary>
    public Modifier AspectRatio(float ratio, bool matchHeightConstraintsFirst = false) =>
        Append(curr => ComposeBridges.ModifierAspectRatio(curr, ratio, matchHeightConstraintsFirst));

    /// <summary>
    /// <c>Modifier.offset(x, y)</c> — shifts the composable's draw
    /// position by (x, y) without affecting the layout slot the parent
    /// allocates for it. Layout-direction-aware (start/end on RTL).
    /// Pass <c>null</c> for either axis to leave it at <c>0.dp</c>.
    /// </summary>
    public Modifier Offset(Dp? x = null, Dp? y = null) =>
        Append(curr => ComposeBridges.ModifierOffset(curr, x, y));

    /// <summary>
    /// <c>Modifier.absoluteOffset(x, y)</c> — like <see cref="Offset"/>
    /// but always uses absolute (left/right) axes, ignoring layout
    /// direction.
    /// </summary>
    public Modifier AbsoluteOffset(Dp? x = null, Dp? y = null) =>
        Append(curr => ComposeBridges.ModifierAbsoluteOffset(curr, x, y));

    /// <summary>
    /// <c>Modifier.zIndex(z)</c> — sets the draw order within a parent
    /// layout. Children with higher <paramref name="z"/> draw on top of
    /// siblings with lower values. Defaults to <c>0f</c>.
    /// </summary>
    public Modifier ZIndex(float z) =>
        Append(curr => ComposeBridges.ModifierZIndex(curr, z));

    /// <summary>
    /// <c>Modifier.alpha(alpha)</c> — applies an alpha multiplier to
    /// the composable's draw output. <c>0f</c> is fully transparent;
    /// <c>1f</c> is fully opaque. Cheap to animate (forces a graphics
    /// layer).
    /// </summary>
    public Modifier Alpha(float alpha) =>
        Append(curr => ComposeBridges.ModifierAlpha(curr, alpha));

    /// <summary>
    /// <c>Modifier.rotate(degrees)</c> — rotates the composable around
    /// its center by <paramref name="degrees"/>. Negative values rotate
    /// counter-clockwise.
    /// </summary>
    public Modifier Rotate(float degrees) =>
        Append(curr => ComposeBridges.ModifierRotate(curr, degrees));

    /// <summary>
    /// <c>Modifier.scale(scale)</c> — uniform scale around the
    /// composable's center.
    /// </summary>
    public Modifier Scale(float scale) =>
        Append(curr => ComposeBridges.ModifierScaleUniform(curr, scale));

    /// <summary>
    /// <c>Modifier.scale(scaleX, scaleY)</c> — independent X / Y scale
    /// around the composable's center.
    /// </summary>
    public Modifier Scale(float scaleX, float scaleY) =>
        Append(curr => ComposeBridges.ModifierScaleXY(curr, scaleX, scaleY));

    /// <summary>
    /// <c>Modifier.shadow(elevation, shape)</c> — draws a soft drop
    /// shadow under the composable. <paramref name="elevation"/> is the
    /// shadow's apparent depth in dp. Pass a non-null
    /// <paramref name="shape"/> to clip the shadow to a custom outline;
    /// otherwise it follows Kotlin's default <c>RectangleShape</c>. The
    /// shape is captured by the closure so its Java peer stays alive
    /// across recompositions. Kotlin's default for the underlying
    /// <c>clip</c> parameter (<c>elevation &gt; 0.dp</c>) is honored.
    /// </summary>
    public Modifier Shadow(Dp elevation, Shape? shape = null)
    {
        var e = elevation.Value;
        return Append(curr => ComposeBridges.ModifierShadow(curr, e, shape?.Handle));
    }

    /// <summary>
    /// <c>Modifier.imePadding()</c> — pads the composable so it sits
    /// above the soft keyboard (IME) when it's visible. Use under
    /// edge-to-edge to keep input controls visible.
    /// </summary>
    public Modifier ImePadding() =>
        Append(curr => ComposeBridges.ModifierImePadding(curr));

    /// <summary>
    /// <c>Modifier.navigationBarsPadding()</c> — pads for the system
    /// navigation bar inset only.
    /// </summary>
    public Modifier NavigationBarsPadding() =>
        Append(curr => ComposeBridges.ModifierNavigationBarsPadding(curr));

    /// <summary>
    /// <c>Modifier.statusBarsPadding()</c> — pads for the status bar
    /// inset only.
    /// </summary>
    public Modifier StatusBarsPadding() =>
        Append(curr => ComposeBridges.ModifierStatusBarsPadding(curr));

    /// <summary>
    /// <c>Modifier.displayCutoutPadding()</c> — pads for display cutouts
    /// (notches, hole-punch cameras) so content doesn't get clipped by
    /// hardware features.
    /// </summary>
    public Modifier DisplayCutoutPadding() =>
        Append(curr => ComposeBridges.ModifierDisplayCutoutPadding(curr));

    /// <summary>
    /// <c>Modifier.testTag(tag)</c> — attaches a stable identifier for
    /// UI testing frameworks (Compose UI Test, Espresso). Has no visual
    /// effect; only affects the semantics tree.
    /// </summary>
    public Modifier TestTag(string tag)
    {
        System.ArgumentNullException.ThrowIfNull(tag);
        return Append(curr => ComposeBridges.ModifierTestTag(curr, tag));
    }

    /// <summary>
    /// Materialize the chain into a managed <c>IModifier</c> wrapper.
    /// Returns <c>null</c> when the chain is empty (no ops appended) so
    /// callers can keep the Kotlin <c>$default</c> bit set and let
    /// Compose substitute the real default.
    /// </summary>
    internal IModifier? Build() => Build(IntPtr.Zero);

    /// <summary>
    /// Materialize the chain into a managed <c>IModifier</c> wrapper,
    /// optionally prepending a <c>Modifier.padding(contentPadding)</c>
    /// op against the supplied <see cref="IntPtr"/> handle. The padding
    /// op runs FIRST, before any user ops — semantically equivalent to
    /// <c>Modifier.padding(contentPadding).then(this)</c> but without
    /// allocating a managed <see cref="Modifier"/> wrapper. Used by
    /// <see cref="ComposableNode.BuildModifier"/> to apply the
    /// <see cref="Scaffold"/>-supplied <c>PaddingValues</c> to the body
    /// node on every measure pass without per-pass allocations
    /// (issue #46).
    /// </summary>
    internal IModifier? Build(IntPtr contentPadding)
    {
        bool hasContentPadding = contentPadding != IntPtr.Zero;
        if (_ops.Length == 0 && !hasContentPadding)
            return null;

        // `current` always holds the latest live local ref. On the
        // happy path we zero it out after handing ownership to
        // GetObject; the finally then no-ops. On any exception path
        // — JNI op throwing, GetObject throwing, anything in between
        // — the finally deletes whichever local ref is still live so
        // we never leak.
        IntPtr current = IntPtr.Zero;
        try
        {
            current = ComposeBridges.ModifierCompanionInstance();
            if (hasContentPadding)
            {
                IntPtr next = ComposeBridges.ModifierPaddingValues(current, contentPadding);
                JNIEnv.DeleteLocalRef(current);
                current = next;
            }
            for (int i = 0; i < _ops.Length; i++)
            {
                IntPtr next = _ops[i](current);
                JNIEnv.DeleteLocalRef(current);
                current = next;
            }

            var result = Java.Lang.Object.GetObject<IModifier>(current, JniHandleOwnership.TransferLocalRef)!;
            current = IntPtr.Zero;
            return result;
        }
        finally
        {
            if (current != IntPtr.Zero)
                JNIEnv.DeleteLocalRef(current);
        }
    }
}
