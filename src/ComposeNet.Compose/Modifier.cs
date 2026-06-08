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
/// adds <see cref="Background(Color)"/>, <see cref="Border(ComposeNet.Dp, Color)"/>,
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
    /// <c>Modifier.minimumInteractiveComponentSize()</c> — reserves at
    /// least 48dp × 48dp around the composable to keep touch targets
    /// accessible. Add to icon-only buttons, dense list rows, or any
    /// other small clickable surface so its hit-box meets the Material
    /// 3 accessibility minimum. Must appear in the chain BEFORE any
    /// <see cref="Size(Dp)"/> / <see cref="Width(Dp)"/> /
    /// <see cref="Height(Dp)"/> modifier that would limit the
    /// composable's constraints, otherwise the reserved space is
    /// clamped away.
    /// </summary>
    /// <remarks>
    /// This modifier only affects layout — it doesn't enable touch
    /// expansion (the platform already does that via
    /// <c>ViewConfiguration</c>). Its job is to make sure neighboring
    /// composables don't crowd the touch region.
    /// </remarks>
    public Modifier MinimumInteractiveComponentSize() =>
        Append(h => ComposeBridges.ModifierMinimumInteractiveComponentSize(h));

    /// <summary>
    /// <c>Modifier.background(color)</c> — paints a flat fill behind the
    /// composable using a <c>RectangleShape</c>. Takes a packed Compose
    /// <c>androidx.compose.ui.graphics.Color</c> value (a <c>ULong</c>
    /// surfaced as a <c>long</c> in the binding because Color is a Kotlin
    /// <c>@JvmInline value class</c>). Build one via
    /// <see cref="ComposeNet.Color.FromRgb(byte, byte, byte)"/>,
    /// <see cref="ComposeNet.Color.FromHex(string)"/>, or one of the
    /// named constants on <see cref="ComposeNet.Color"/> (e.g.
    /// <see cref="ComposeNet.Color.Black"/>).
    /// </summary>
    public Modifier Background(Color color) =>
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
    public Modifier Background(Color color, Shape? shape) =>
        Append(curr => ComposeBridges.ModifierBackground(curr, color, shape?.Handle));

    /// <summary>
    /// <c>Modifier.border(width, color)</c> — draws a rectangular stroke
    /// around the composable. <paramref name="width"/> is the stroke
    /// width in density-independent pixels. For rounded corners use
    /// <see cref="Border(Dp, Color, Dp)"/>; for arbitrary shapes use
    /// <see cref="Border(Dp, Color, Shape?)"/>.
    /// </summary>
    public Modifier Border(Dp width, Color color)
    {
        var w = width.Value;
        long c = color;
        return Append(curr => ComposeBridges.ModifierBorder(curr, w, c, null));
    }

    /// <summary>
    /// <c>Modifier.border(width, color, RoundedCornerShape(cornerRadius))</c> —
    /// draws a stroke with rounded corners. Match
    /// <paramref name="cornerRadius"/> to a <see cref="Clip(Dp)"/>
    /// earlier in the chain so the corners align (otherwise the
    /// rectangular stroke gets sliced by a rounded clip and you see
    /// jagged corner stubs).
    /// </summary>
    public Modifier Border(Dp width, Color color, Dp cornerRadius)
    {
        var w = width.Value;
        var r = cornerRadius.Value;
        long c = color;
        if (r <= 0f)
            return Append(curr => ComposeBridges.ModifierBorder(curr, w, c, null));

        return Append(curr =>
        {
            IntPtr shape = ComposeBridges.RoundedCornerShape(r);
            try
            {
                return ComposeBridges.ModifierBorder(curr, w, c, shape);
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
    public Modifier Border(Dp width, Color color, Shape? shape)
    {
        var w = width.Value;
        long c = color;
        return Append(curr => ComposeBridges.ModifierBorder(curr, w, c, shape?.Handle));
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
    /// <see cref="ComposeActivity.Remember{T}(System.Func{T}, int, string)"/>:
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
    /// <c>Modifier.draggable(state, orientation, enabled)</c> — drag
    /// gesture handler that reports raw drag deltas (in pixels along
    /// the chosen <see cref="Orientation"/>) to the supplied
    /// <see cref="DraggableState"/>. Unlike scroll, this modifier does
    /// not consume the deltas itself — your <c>onDelta</c> callback
    /// inside <see cref="DraggableState"/> decides what to do with the
    /// movement (typically: update an offset state that another
    /// modifier reads).
    /// </summary>
    /// <param name="state">State holder that receives drag deltas.
    /// Build via <c>new DraggableState(delta =&gt; ...)</c> inside a
    /// <see cref="Compose.Remember{T}(System.Func{T}, int, string)"/> call, or via
    /// <see cref="Compose.RememberDraggableState(System.Action{float}, int, string)"/>
    /// for stable Java identity across recompositions when the
    /// callback closure changes.</param>
    /// <param name="orientation">Axis the gesture operates on —
    /// <see cref="Orientation.Vertical"/> for up-down drags,
    /// <see cref="Orientation.Horizontal"/> for left-right drags.</param>
    /// <param name="enabled">When <c>false</c>, the modifier ignores
    /// touch input. Defaults to <c>true</c>.</param>
    public Modifier Draggable(DraggableState state, Orientation orientation, bool enabled = true)
    {
        System.ArgumentNullException.ThrowIfNull(state);
        var jvm = state.Jvm;
        var jvmOrientation = orientation == Orientation.Horizontal
            ? AndroidX.Compose.Foundation.Gestures.Orientation.Horizontal!
            : AndroidX.Compose.Foundation.Gestures.Orientation.Vertical!;
        return Append(curr =>
            ComposeBridges.ModifierDraggable(
                curr,
                ((Java.Lang.Object)jvm).Handle,
                ((Java.Lang.Object)jvmOrientation).Handle,
                enabled));
    }

    /// <summary>
    /// <c>Modifier.nestedScroll(connection)</c> — bridge a scrollable
    /// container to a nested-scrolling parent (most commonly a
    /// Material 3 <see cref="TopAppBar"/> / <see cref="MediumTopAppBar"/>
    /// / <see cref="LargeTopAppBar"/> that collapses on scroll).
    /// Apply this modifier to the scrolling container — usually a
    /// <c>LazyColumn</c>, <c>LazyRow</c>, or a <see cref="Column"/>
    /// with <c>VerticalScroll(ScrollState)</c> — and Compose will
    /// route scroll deltas through <paramref name="connection"/> so
    /// the parent can consume some of them before the container does.
    /// </summary>
    /// <param name="connection">
    /// The connection to forward scroll deltas to. Pair with a
    /// <see cref="TopAppBarScrollBehavior"/> by passing
    /// <see cref="TopAppBarScrollBehavior.NestedScrollConnection"/>;
    /// the scroll behavior is also set as the bar's
    /// <c>ScrollBehavior</c> property so both sides agree on the
    /// shared <see cref="TopAppBarState"/>.
    /// </param>
    public Modifier NestedScroll(NestedScrollConnection connection)
    {
        System.ArgumentNullException.ThrowIfNull(connection);
        IntPtr handle = connection.Handle;
        return Append(curr =>
        {
            try
            {
                return ComposeBridges.ModifierNestedScroll(curr, handle);
            }
            finally
            {
                System.GC.KeepAlive(connection);
            }
        });
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
    /// <c>Modifier.graphicsLayer(...)</c> — applies a draw-time
    /// graphics layer to the composable. All parameters default to
    /// <c>null</c>, which lets Compose substitute its own
    /// per-property defaults (identity for transforms, full opacity
    /// for <paramref name="alpha"/>, etc.). Supply only the
    /// properties you want to override.
    /// </summary>
    /// <param name="scaleX">Horizontal scale (1.0 = no scaling).</param>
    /// <param name="scaleY">Vertical scale (1.0 = no scaling).</param>
    /// <param name="alpha">Opacity in [0, 1].</param>
    /// <param name="translationX">Horizontal translation in pixels.</param>
    /// <param name="translationY">Vertical translation in pixels.</param>
    /// <param name="shadowElevation">Elevation in pixels (use <see cref="Shadow"/> to specify in Dp).</param>
    /// <param name="rotationX">Rotation around the X axis in degrees.</param>
    /// <param name="rotationY">Rotation around the Y axis in degrees.</param>
    /// <param name="rotationZ">Rotation around the Z axis in degrees (clockwise).</param>
    /// <param name="cameraDistance">Distance of the camera from the rotation pivot, in pixels.</param>
    /// <param name="transformOrigin">Packed <c>TransformOrigin</c> value (use <see cref="ComposeNet.TransformOrigin.Pack(float, float)"/>); the default is the center (0.5, 0.5).</param>
    /// <param name="shape">Clip / shadow outline shape (default = rectangle).</param>
    /// <param name="clip">Whether to clip content to <paramref name="shape"/>.</param>
    public Modifier GraphicsLayer(
        float? scaleX = null,
        float? scaleY = null,
        float? alpha = null,
        float? translationX = null,
        float? translationY = null,
        float? shadowElevation = null,
        float? rotationX = null,
        float? rotationY = null,
        float? rotationZ = null,
        float? cameraDistance = null,
        long? transformOrigin = null,
        Shape? shape = null,
        bool? clip = null)
    {
        return Append(curr => ComposeBridges.ModifierGraphicsLayer(
            curr,
            scaleX,
            scaleY,
            alpha,
            translationX,
            translationY,
            shadowElevation,
            rotationX,
            rotationY,
            rotationZ,
            cameraDistance,
            transformOrigin,
            shape?.Handle,
            clip));
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
    /// <c>Modifier.captionBarPadding()</c> — pads for the caption bar
    /// inset (window decorations on freeform / desktop windowing modes).
    /// On phones without a caption bar this is a no-op.
    /// </summary>
    public Modifier CaptionBarPadding() =>
        Append(curr => ComposeBridges.ModifierCaptionBarPadding(curr));

    /// <summary>
    /// <c>Modifier.mandatorySystemGesturesPadding()</c> — pads for the
    /// subset of gesture insets the system always reserves for itself
    /// (e.g. the bottom home-gesture strip), even when the user opts
    /// out of edge gestures.
    /// </summary>
    public Modifier MandatorySystemGesturesPadding() =>
        Append(curr => ComposeBridges.ModifierMandatorySystemGesturesPadding(curr));

    /// <summary>
    /// <c>Modifier.safeContentPadding()</c> — union of
    /// <see cref="SafeDrawingPadding"/> and
    /// <see cref="SafeGesturesPadding"/>. Use for content that should
    /// avoid both visual obstructions and gesture zones.
    /// </summary>
    public Modifier SafeContentPadding() =>
        Append(curr => ComposeBridges.ModifierSafeContentPadding(curr));

    /// <summary>
    /// <c>Modifier.safeGesturesPadding()</c> — union of
    /// <see cref="MandatorySystemGesturesPadding"/> +
    /// <see cref="SystemGesturesPadding"/> + the tappable-element
    /// insets. Use to keep interactive UI out of the system's gesture
    /// zones.
    /// </summary>
    public Modifier SafeGesturesPadding() =>
        Append(curr => ComposeBridges.ModifierSafeGesturesPadding(curr));

    /// <summary>
    /// <c>Modifier.systemGesturesPadding()</c> — pads for the system
    /// gesture insets (the edge regions where the OS may interpret
    /// swipes as system gestures such as back / home).
    /// </summary>
    public Modifier SystemGesturesPadding() =>
        Append(curr => ComposeBridges.ModifierSystemGesturesPadding(curr));

    /// <summary>
    /// <c>Modifier.waterfallPadding()</c> — pads for waterfall display
    /// insets (the curved edges of waterfall-screen devices). No-op on
    /// flat-screen phones.
    /// </summary>
    public Modifier WaterfallPadding() =>
        Append(curr => ComposeBridges.ModifierWaterfallPadding(curr));

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
    /// <c>Modifier.align(alignment)</c> — positions the child within
    /// a parent <see cref="Box"/>. Only valid inside a <see cref="Box"/>
    /// (any container that publishes <see cref="ScopeKind.Box"/>).
    /// </summary>
    public Modifier Align(Alignment alignment)
    {
        System.ArgumentNullException.ThrowIfNull(alignment);
        return Append(curr =>
        {
            IntPtr scope = RenderContext.CurrentScope;
            if (RenderContext.CurrentScopeKind != ScopeKind.Box)
                throw new System.InvalidOperationException(
                    "Modifier.Align(Alignment) is only valid inside a Box. " +
                    $"Current scope kind: {RenderContext.CurrentScopeKind}.");
            return ComposeBridges.BoxScopeAlign(scope, curr, ((Java.Lang.Object)alignment).Handle);
        });
    }

    /// <summary>
    /// <c>Modifier.align(alignment)</c> for a Row child — aligns the
    /// child vertically within the row. Only valid inside a
    /// <see cref="Row"/>.
    /// </summary>
    public Modifier Align(Alignment.Vertical alignment)
    {
        System.ArgumentNullException.ThrowIfNull(alignment);
        return Append(curr =>
        {
            IntPtr scope = RenderContext.CurrentScope;
            if (RenderContext.CurrentScopeKind != ScopeKind.Row)
                throw new System.InvalidOperationException(
                    "Modifier.Align(Alignment.Vertical) is only valid inside a Row. " +
                    $"Current scope kind: {RenderContext.CurrentScopeKind}.");
            return ComposeBridges.RowScopeAlignVertical(scope, curr, ((Java.Lang.Object)alignment.Java).Handle);
        });
    }

    /// <summary>
    /// <c>Modifier.align(alignment)</c> for a Column child — aligns the
    /// child horizontally within the column. Only valid inside a
    /// <see cref="Column"/>.
    /// </summary>
    public Modifier Align(Alignment.Horizontal alignment)
    {
        System.ArgumentNullException.ThrowIfNull(alignment);
        return Append(curr =>
        {
            IntPtr scope = RenderContext.CurrentScope;
            if (RenderContext.CurrentScopeKind != ScopeKind.Column)
                throw new System.InvalidOperationException(
                    "Modifier.Align(Alignment.Horizontal) is only valid inside a Column. " +
                    $"Current scope kind: {RenderContext.CurrentScopeKind}.");
            return ComposeBridges.ColumnScopeAlignHorizontal(scope, curr, ((Java.Lang.Object)alignment.Java).Handle);
        });
    }

    /// <summary>
    /// <c>Modifier.matchParentSize()</c> — sizes the child to match
    /// the parent <see cref="Box"/>'s measured size without
    /// participating in measurement. Only valid inside a
    /// <see cref="Box"/>.
    /// </summary>
    public Modifier MatchParentSize() =>
        Append(curr =>
        {
            IntPtr scope = RenderContext.CurrentScope;
            if (RenderContext.CurrentScopeKind != ScopeKind.Box)
                throw new System.InvalidOperationException(
                    "Modifier.MatchParentSize() is only valid inside a Box. " +
                    $"Current scope kind: {RenderContext.CurrentScopeKind}.");
            return ComposeBridges.BoxScopeMatchParentSize(scope, curr);
        });

    /// <summary>
    /// <c>Modifier.focusable(enabled = true)</c> — marks the node as a
    /// focus target. Combine with <see cref="FocusRequester(ComposeNet.FocusRequester)"/>
    /// to programmatically move focus, or with
    /// <see cref="OnFocusChanged(System.Action{ComposeNet.FocusState})"/>
    /// to observe focus changes.
    /// </summary>
    public Modifier Focusable(bool enabled = true) =>
        Append(curr => ComposeBridges.ModifierFocusable(curr, enabled));

    /// <summary>
    /// <c>Modifier.focusGroup()</c> — groups focusable descendants so
    /// two-dimensional focus search treats them as a single unit.
    /// </summary>
    public Modifier FocusGroup() =>
        Append(curr => ComposeBridges.ModifierFocusGroup(curr));

    /// <summary>
    /// <c>Modifier.onFocusChanged { ... }</c> — invokes <paramref name="onFocusChanged"/>
    /// whenever the node gains, loses, or has its focus state mutated
    /// (capture / release). The callback receives an immutable
    /// <see cref="FocusState"/> snapshot.
    /// </summary>
    public Modifier OnFocusChanged(System.Action<FocusState> onFocusChanged)
    {
        System.ArgumentNullException.ThrowIfNull(onFocusChanged);
        var f1 = new ComposableLambda1(arg =>
        {
            if (arg is null) return;
            var fs = Android.Runtime.Extensions.JavaCast<AndroidX.Compose.UI.Focus.IFocusState>(arg);
            onFocusChanged(FocusState.From(fs));
        });
        return Append(curr => ComposeBridges.ModifierOnFocusChanged(curr, f1));
    }

    /// <summary>
    /// <c>Modifier.focusRequester(requester)</c> — installs
    /// <paramref name="requester"/> on the node so the caller can
    /// programmatically move focus by calling
    /// <see cref="ComposeNet.FocusRequester.RequestFocus"/>.
    /// </summary>
    public Modifier FocusRequester(FocusRequester requester)
    {
        System.ArgumentNullException.ThrowIfNull(requester);
        return Append(curr =>
            ComposeBridges.ModifierFocusRequester(curr, ((Java.Lang.Object)requester.Java).Handle));
    }

    /// <summary>
    /// <c>Modifier.combinedClickable(...)</c> — clickable with optional
    /// long-press and double-tap handlers. <paramref name="onClick"/> is
    /// required; pass <c>null</c> for either of the other two to fall
    /// back to Kotlin's "ignore that gesture" defaults.
    /// </summary>
    public Modifier CombinedClickable(
        System.Action onClick,
        System.Action? onLongClick = null,
        System.Action? onDoubleClick = null)
    {
        System.ArgumentNullException.ThrowIfNull(onClick);
        var click = new ComposableLambda0(onClick);
        var longClick = onLongClick is null ? null : new ComposableLambda0(onLongClick);
        var doubleClick = onDoubleClick is null ? null : new ComposableLambda0(onDoubleClick);
        return Append(curr =>
            ComposeBridges.ModifierCombinedClickable(curr, longClick, doubleClick, click));
    }

    /// <summary>
    /// <c>Modifier.selectable(selected, onClick)</c> — marks the node
    /// as a selectable choice in a single-selection group (e.g. a
    /// radio button group). Sets up the right accessibility semantics
    /// and forwards taps to <paramref name="onClick"/>.
    /// </summary>
    public Modifier Selectable(bool selected, System.Action onClick)
    {
        System.ArgumentNullException.ThrowIfNull(onClick);
        var click = new ComposableLambda0(onClick);
        return Append(curr =>
            ComposeBridges.ModifierSelectable(curr, selected, click));
    }

    /// <summary>
    /// <c>Modifier.toggleable(value, onValueChange)</c> — marks the
    /// node as a binary toggle (e.g. a checkbox row). Sets up the
    /// right accessibility semantics and forwards taps to
    /// <paramref name="onValueChange"/> with the negated value.
    /// </summary>
    public Modifier Toggleable(bool value, System.Action<bool> onValueChange)
    {
        System.ArgumentNullException.ThrowIfNull(onValueChange);
        var f1 = new ComposableLambda1(arg =>
        {
            bool v = arg is Java.Lang.Boolean jb && jb.BooleanValue();
            onValueChange(v);
        });
        return Append(curr =>
            ComposeBridges.ModifierToggleable(curr, value, f1));
    }

    /// <summary>
    /// <c>Modifier.semantics { contentDescription = ... }</c> — adds
    /// a content description for accessibility (TalkBack reads it
    /// aloud when the node is focused). Doesn't merge descendant
    /// semantics; call the overload taking <c>mergeDescendants</c> for
    /// that.
    /// </summary>
    public Modifier Semantics(string contentDescription) =>
        Semantics(mergeDescendants: false, contentDescription, role: null);

    /// <summary>
    /// <c>Modifier.semantics(mergeDescendants) { contentDescription = ... }</c> —
    /// set <paramref name="mergeDescendants"/> to <c>true</c> for a
    /// container that should announce itself instead of its children
    /// (e.g. a card with a label and a value).
    /// </summary>
    public Modifier Semantics(bool mergeDescendants, string contentDescription) =>
        Semantics(mergeDescendants, contentDescription, role: null);

    /// <summary>
    /// <c>Modifier.semantics { role = ... }</c> — tags the node with
    /// an accessibility <see cref="SemanticsRole"/> (e.g.
    /// <see cref="SemanticsRole.Button"/>). Useful when wrapping a
    /// custom composable that's clickable but isn't a real
    /// <see cref="Button"/>, so TalkBack announces "button" instead
    /// of just the content description.
    /// </summary>
    public Modifier Semantics(SemanticsRole role) =>
        Semantics(mergeDescendants: false, contentDescription: null, role: role);

    /// <summary>
    /// <c>Modifier.semantics { contentDescription = ...; role = ... }</c> —
    /// combine a content description with a role in a single
    /// semantics block. Either argument may be omitted by passing
    /// <c>null</c>.
    /// </summary>
    public Modifier Semantics(string? contentDescription, SemanticsRole? role) =>
        Semantics(mergeDescendants: false, contentDescription, role);

    /// <summary>
    /// <c>Modifier.semantics(mergeDescendants) { contentDescription = ...; role = ... }</c> —
    /// full form. Pass <c>null</c> for either property to skip it.
    /// At least one of <paramref name="contentDescription"/> or
    /// <paramref name="role"/> must be supplied.
    /// </summary>
    public Modifier Semantics(bool mergeDescendants, string? contentDescription, SemanticsRole? role)
    {
        if (contentDescription is null && role is null)
            throw new System.ArgumentException(
                "At least one of contentDescription or role must be supplied.",
                nameof(contentDescription));
        var properties = new ComposableLambda1(arg =>
        {
            if (arg is Java.Lang.Object obj)
            {
                if (contentDescription is not null)
                    ComposeBridges.SemanticsSetContentDescription(obj.Handle, contentDescription);
                if (role is not null)
                    ComposeBridges.SemanticsSetRole(obj.Handle, (int)role.Value);
            }
        });
        return Append(curr =>
            ComposeBridges.ModifierSemantics(curr, mergeDescendants, properties));
    }

    /// <summary>
    /// <c>Modifier.clearAndSetSemantics { contentDescription = ... }</c> —
    /// like <see cref="Semantics(string)"/>, but discards the
    /// descendant semantics first. Use when a custom composable
    /// should appear as a single accessibility node with a curated
    /// description, hiding implementation details.
    /// </summary>
    public Modifier ClearAndSetSemantics(string contentDescription) =>
        ClearAndSetSemantics(contentDescription, role: null);

    /// <summary>
    /// <c>Modifier.clearAndSetSemantics { contentDescription = ...; role = ... }</c> —
    /// combine a content description with a <see cref="SemanticsRole"/>
    /// in a single clear-and-set block. At least one of
    /// <paramref name="contentDescription"/> or <paramref name="role"/>
    /// must be supplied.
    /// </summary>
    public Modifier ClearAndSetSemantics(string? contentDescription, SemanticsRole? role)
    {
        if (contentDescription is null && role is null)
            throw new System.ArgumentException(
                "At least one of contentDescription or role must be supplied.",
                nameof(contentDescription));
        var properties = new ComposableLambda1(arg =>
        {
            if (arg is Java.Lang.Object obj)
            {
                if (contentDescription is not null)
                    ComposeBridges.SemanticsSetContentDescription(obj.Handle, contentDescription);
                if (role is not null)
                    ComposeBridges.SemanticsSetRole(obj.Handle, (int)role.Value);
            }
        });
        return Append(curr =>
            ComposeBridges.ModifierClearAndSetSemantics(curr, properties));
    }

    /// <summary>
    /// <c>Modifier.pointerInput(Unit) { detectTapGestures(...) }</c> —
    /// detect the four basic single-pointer tap gestures (tap, press,
    /// long-press, double-tap) and invoke the supplied C# callbacks on
    /// the UI thread with the tap position as an <see cref="Offset"/>
    /// in local layout pixels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All four callback parameters are optional; pass only the ones
    /// you care about. When all four are <c>null</c> this modifier is
    /// a no-op (but still allocates the per-pointer-input state
    /// holder, so prefer omitting it entirely in that case).
    /// </para>
    /// <para>
    /// Unlike <see cref="Clickable(System.Action)"/>, this modifier
    /// supplies <strong>no Material indication / ripple</strong>, no
    /// accessibility semantics, and no role — it's the low-level
    /// gesture primitive. Use <see cref="Clickable(System.Action)"/>
    /// for ordinary clickable surfaces; reach for
    /// <see cref="DetectTapGestures"/> when you need long-press,
    /// double-tap, or precise tap positions.
    /// </para>
    /// <para>
    /// <strong>Callback freshness gotcha.</strong> The Kotlin
    /// <c>pointerInput</c> modifier only restarts its coroutine when
    /// its key changes — <em>not</em> when the handler instance
    /// changes. Because all C# callback adapters share the same Java
    /// class, simply rebuilding this modifier with different lambdas
    /// will NOT pick up the new callbacks. To refresh, either
    /// (a) supply a varying <paramref name="key"/> derived from the
    /// values your callbacks close over, or (b) have the callbacks
    /// read mutable state via a remembered
    /// <c>MutableState&lt;T&gt;</c> so they pick up new values on
    /// each invocation.
    /// </para>
    /// </remarks>
    /// <param name="onTap">Fired once the system confirms the gesture
    /// is a simple tap (not the start of a long-press or double-tap).</param>
    /// <param name="onPress">Fired the moment the finger goes down,
    /// before the system has decided which gesture this will become.
    /// Useful for "pressed" visual feedback.</param>
    /// <param name="onLongPress">Fired after the system's
    /// long-press timeout (typically ~500ms) while the finger is still
    /// down.</param>
    /// <param name="onDoubleTap">Fired when a second tap arrives
    /// within the system double-tap timeout (typically ~300ms) of the
    /// first.</param>
    /// <param name="key">Identity key passed to Kotlin's
    /// <c>pointerInput</c>. Defaults to a stable singleton
    /// (<c>Kotlin.Unit</c>), so the gesture detector coroutine starts
    /// once and never restarts. Pass any value whose <c>Equals</c>
    /// changes when callbacks should reset.</param>
    public Modifier DetectTapGestures(
        System.Action<Offset>? onTap = null,
        System.Action<Offset>? onPress = null,
        System.Action<Offset>? onLongPress = null,
        System.Action<Offset>? onDoubleTap = null,
        object? key = null)
    {
        var tapCb = onTap is null ? null : new OffsetCallback(onTap);
        var pressCb = onPress is null ? null : new OffsetPressCallback(onPress);
        var longPressCb = onLongPress is null ? null : new OffsetCallback(onLongPress);
        var doubleTapCb = onDoubleTap is null ? null : new OffsetCallback(onDoubleTap);

        var block = new PointerInputBlock(tapCb, pressCb, longPressCb, doubleTapCb);

        // Resolve `key` to a Java object. Kotlin uses reference equality
        // for the default-overload form, so we want a stable JNI object
        // for the "no key" path — Kotlin.Unit.Instance is the canonical
        // singleton.
        var keyObj = key switch
        {
            null => (Java.Lang.Object)global::Kotlin.Unit.Instance!,
            Java.Lang.Object jlo => jlo,
            string s => new Java.Lang.String(s),
            int i => Java.Lang.Integer.ValueOf(i),
            long l => Java.Lang.Long.ValueOf(l),
            bool b => Java.Lang.Boolean.ValueOf(b),
            _ => new Java.Lang.String(key.ToString() ?? ""),
        };

        return Append(curr =>
        {
            // Construct the Java helper that implements
            // PointerInputEventHandler, wrapping our Function2 JCW.
            // The handler is a local ref consumed by ModifierPointerInput
            // and released when its JNI frame pops.
            var handlerLocal = System.IntPtr.Zero;
            try
            {
                handlerLocal = ComposeBridges.NewPointerInputEventHandler(
                    ((Java.Lang.Object)block).Handle);
                return ComposeBridges.ModifierPointerInput(
                    curr, keyObj.Handle, handlerLocal);
            }
            finally
            {
                if (handlerLocal != System.IntPtr.Zero)
                    Android.Runtime.JNIEnv.DeleteLocalRef(handlerLocal);
                System.GC.KeepAlive(keyObj);
                System.GC.KeepAlive(block);
            }
        });
    }

    /// <summary>
    /// <c>Modifier.semantics { ... }</c> — fluent builder form that
    /// exposes all supported semantic properties (selected, role,
    /// content/state description, onClick label) on
    /// <see cref="SemanticsScope"/>. Doesn't merge descendant
    /// semantics; use the overload taking <paramref name="properties"/>
    /// + <c>mergeDescendants</c> for that.
    /// </summary>
    /// <param name="properties">Builder callback. Each chained call on
    /// the supplied <see cref="SemanticsScope"/> sets one property on
    /// the underlying Kotlin <c>SemanticsPropertyReceiver</c>.</param>
    public Modifier Semantics(System.Action<SemanticsScope> properties) =>
        Semantics(mergeDescendants: false, properties);

    /// <summary>
    /// <c>Modifier.semantics(mergeDescendants) { ... }</c> — fluent
    /// builder form. Set <paramref name="mergeDescendants"/> to
    /// <c>true</c> for a container that should announce itself as a
    /// single accessibility node instead of exposing its children
    /// (e.g. a card with a label and a value).
    /// </summary>
    /// <param name="mergeDescendants">Whether to merge descendant
    /// semantics into this node.</param>
    /// <param name="properties">Builder callback. See
    /// <see cref="SemanticsScope"/> for available property setters.</param>
    public Modifier Semantics(bool mergeDescendants, System.Action<SemanticsScope> properties)
    {
        System.ArgumentNullException.ThrowIfNull(properties);
        var lambda = WrapSemanticsBuilder(properties);
        return Append(curr =>
            ComposeBridges.ModifierSemantics(curr, mergeDescendants, lambda));
    }

    /// <summary>
    /// <c>Modifier.clearAndSetSemantics { ... }</c> — fluent builder
    /// form. Discards the descendant semantics first, then applies
    /// the properties set in <paramref name="properties"/>. Use when
    /// a custom composable should appear as a single accessibility
    /// node with a curated description, hiding implementation
    /// details.
    /// </summary>
    /// <param name="properties">Builder callback. See
    /// <see cref="SemanticsScope"/> for available property setters.</param>
    public Modifier ClearAndSetSemantics(System.Action<SemanticsScope> properties)
    {
        System.ArgumentNullException.ThrowIfNull(properties);
        var lambda = WrapSemanticsBuilder(properties);
        return Append(curr =>
            ComposeBridges.ModifierClearAndSetSemantics(curr, lambda));
    }

    // Shared body for the two builder-form overloads above. Each
    // invocation of the returned ComposableLambda1 gets a fresh
    // SemanticsScope bound to the JNI handle Compose hands us, and
    // the scope is Invalidate()d the moment the user callback returns
    // so leaked references throw a clear error on reuse.
    static ComposableLambda1 WrapSemanticsBuilder(System.Action<SemanticsScope> properties) =>
        new(arg =>
        {
            if (arg is Java.Lang.Object obj)
            {
                var scope = new SemanticsScope(obj.Handle);
                try { properties(scope); }
                finally { scope.Invalidate(); }
            }
        });

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
