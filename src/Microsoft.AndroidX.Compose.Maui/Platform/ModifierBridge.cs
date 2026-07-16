using AndroidX.Compose;
using Microsoft.Maui.Controls.Shapes;
using ComposeShape = AndroidX.Compose.Shape;
using MauiVisibility = Microsoft.Maui.Visibility;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// Translates the cross-platform <see cref="IView"/> visual / transform
/// properties (<c>IsVisible</c>, <c>Opacity</c>, <c>Translation</c>,
/// <c>Scale</c>, <c>Rotation</c>, <c>AnchorX/Y</c>, <c>Clip</c>,
/// <c>Shadow</c>) into a Compose modifier chain. Every Compose-backed
/// handler chains <see cref="ApplyViewProperties"/> on its outermost
/// (prepended) modifier so MAUI's struct-typed view-level properties
/// take effect uniformly without each handler re-deriving the
/// translation.
/// </summary>
/// <remarks>
/// <para>This is the Compose analog of MAUI's stock per-property
/// extensions (<c>UpdateOpacity</c>, <c>UpdateTranslationX</c>, …) on
/// <c>Microsoft.Maui.Platform.ViewExtensions</c>. The stock pipeline
/// pokes the Android <c>View</c> directly; for our handlers the view
/// is a detached <c>ComposeView</c> on the leaf path (folded into the
/// parent composition via <see cref="IComposeHandler.BuildNode"/>) so
/// modifying its <c>Alpha</c> / <c>TranslationX</c> / … is invisible.
/// We model the same effects via Compose modifiers instead.</para>
///
/// <para><b>Recomposition:</b> mapper writes go into one shared
/// <c>MutableState&lt;int&gt;</c> view-properties version slot on
/// <see cref="ComposeElementHandler{TVirtualView}"/>; <c>BuildNode</c>
/// implementations subscribe by reading that slot inside the
/// composition, then call <see cref="ApplyViewProperties"/> which
/// reads the live struct values off the virtual view. The
/// version-counter pattern is the only option for struct-valued
/// properties (Color / Geometry / Shadow) because
/// <c>MutableState&lt;T&gt;</c> only accepts primitives, strings, and
/// <c>Java.Lang.Object</c> subclasses.</para>
///
/// <para><b>IsVisible semantic choice — alpha vs layout-skip.</b> MAUI's
/// <c>IsVisible == false</c> on the cross-platform side maps to
/// <see cref="MauiVisibility.Hidden"/> on the IView surface (which
/// stock backends collapse via the platform's visibility flag). We
/// translate to <c>Modifier.Alpha(0f)</c>, which preserves the cell's
/// layout space — a transparent placeholder rather than a removed
/// node. This is the closest single-modifier match because Compose's
/// modifier system can't drop a child out of the parent layout slot;
/// dropping the node would require a measure-policy short-circuit
/// (a generic <c>Layout {}</c> adapter, on the Phase 5 roadmap). For
/// now <c>IsVisible == false</c> hides the pixels but leaves the
/// space — matching MAUI's <c>Hidden</c>, not <c>Collapsed</c>,
/// semantics.</para>
///
/// <para><b>3D rotation round-trip.</b> Compose's
/// <c>Modifier.GraphicsLayer { rotationX, rotationY, rotationZ }</c>
/// uses a perspective camera (<c>cameraDistance</c> defaults to ~8 ×
/// the layout depth in dp). MAUI's <c>RotationX</c>/<c>RotationY</c>
/// were originally modelled on a similar perspective transform but
/// with a different camera distance, so a 60° <c>RotationX</c>
/// rendered through Compose's graphics layer foreshortens differently
/// than the same value on the stock Android renderer. The visual
/// difference is most apparent above ~30° of axis tilt; we accept it
/// because matching the stock perspective would require porting
/// Android's <c>android.graphics.Camera</c> matrix into Compose
/// space — out of scope for the bridge.</para>
/// </remarks>
internal static class ModifierBridge
{
    /// <summary>
    /// Chain Compose modifier ops onto <paramref name="modifier"/> for
    /// every <see cref="IView"/> visual / transform property the view
    /// has set to a non-identity value. Returns
    /// <paramref name="modifier"/> unchanged when the view's transform
    /// state is the all-identity default (no allocation, no JNI calls
    /// — common case).
    /// </summary>
    /// <param name="modifier">Starting modifier; usually
    /// <c>Modifier.Companion</c> for the prepended slot.</param>
    /// <param name="view">The MAUI virtual view whose properties feed
    /// the modifier chain.</param>
    /// <returns>A modifier with one or more of <c>Alpha</c>,
    /// <c>Offset</c>, <c>Scale</c>, <c>Rotate</c>,
    /// <c>GraphicsLayer</c>, <c>Clip</c>, <c>Shadow</c> appended in
    /// that order. Order matters: alpha and translation go outermost
    /// (so they affect the entire visual including any clip / shadow
    /// outline), shadow and clip go innermost (so they outline the
    /// drawn content rather than the post-translate position).</returns>
    internal static Modifier ApplyViewProperties(this Modifier modifier, IView view)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        ArgumentNullException.ThrowIfNull(view);

        // -- Visibility / Opacity (outermost so they apply to the
        //    entire box including any clipped / shadowed children).
        // IView.Visibility (Visible / Hidden / Collapsed) is what
        // mappers see. Hidden + Collapsed both render invisibly; we
        // model both as Alpha(0f) and document the trade-off (see
        // class remarks). Opacity stacks: a Visible view with
        // Opacity=0.5 still gets Alpha(0.5).
        if (view.Visibility != MauiVisibility.Visible)
        {
            modifier = modifier.Alpha(0f);
        }
        else if (view.Opacity < 1d)
        {
            modifier = modifier.Alpha((float)Math.Max(0d, view.Opacity));
        }

        // -- Translation. Skip at zero to keep the modifier chain
        //    minimal in the common case.
        var tx = view.TranslationX;
        var ty = view.TranslationY;
        if (tx != 0d || ty != 0d)
        {
            modifier = modifier.Offset(
                x: tx != 0d ? new Dp((float)tx) : default,
                y: ty != 0d ? new Dp((float)ty) : default);
        }

        // -- Scale. MAUI multiplies the uniform Scale by the per-axis
        //    ScaleX / ScaleY, so a Scale=2 ScaleY=0.5 view ends up at
        //    (2, 1).
        var scale  = view.Scale;
        var scaleX = view.ScaleX * scale;
        var scaleY = view.ScaleY * scale;
        if (scaleX != 1d || scaleY != 1d)
        {
            if (Math.Abs(scaleX - scaleY) < 1e-6)
                modifier = modifier.Scale((float)scaleX);
            else
                modifier = modifier.Scale((float)scaleX, (float)scaleY);
        }

        // -- Rotation. The 2D fast path uses Modifier.Rotate (cheaper,
        //    no GraphicsLayer allocation). When the view exercises any
        //    of the 3D-rotation knobs (RotationX / RotationY) or moves
        //    the pivot away from center (AnchorX / AnchorY != 0.5), we
        //    fall back to a single GraphicsLayer that bundles them all
        //    together — Compose only honours one transformOrigin per
        //    layer, so two separate Rotate + GraphicsLayer ops would
        //    pivot inconsistently.
        var rotation  = view.Rotation;
        var rotationX = view.RotationX;
        var rotationY = view.RotationY;
        var anchorX   = view.AnchorX;
        var anchorY   = view.AnchorY;
        bool needsLayer = rotationX != 0d || rotationY != 0d
            || Math.Abs(anchorX - 0.5d) > 1e-6 || Math.Abs(anchorY - 0.5d) > 1e-6;

        if (needsLayer)
        {
            TransformOrigin? origin = (Math.Abs(anchorX - 0.5d) > 1e-6 || Math.Abs(anchorY - 0.5d) > 1e-6)
                ? new TransformOrigin((float)anchorX, (float)anchorY)
                : null;
            modifier = modifier.GraphicsLayer(
                rotationX:       rotationX != 0d ? (float)rotationX : null,
                rotationY:       rotationY != 0d ? (float)rotationY : null,
                rotationZ:       rotation  != 0d ? (float)rotation  : null,
                transformOrigin: origin);
        }
        else if (rotation != 0d)
        {
            modifier = modifier.Rotate((float)rotation);
        }

        // -- Clip + Shadow share a translated shape so the shadow
        //    outline matches the clip (otherwise a rounded clip with
        //    a default-rectangular shadow produces a visible hard-edge
        //    halo behind round-cornered content). Compute once, pass
        //    to both.
        ComposeShape? clipShape = view.Clip is IShape clip
            ? TranslateClipToShape(clip)
            : null;

        // -- Shadow. Innermost so it outlines the drawn content.
        //    Compose's Modifier.Shadow only exposes elevation + shape;
        //    MAUI's Brush / Offset / Opacity beyond the elevation tint
        //    aren't surfaced (Compose synthesises them from the
        //    elevation + the surrounding theme).
        if (view.Shadow is { } shadow && shadow.Radius > 0)
        {
            modifier = modifier.Shadow(new Dp(shadow.Radius), clipShape);
        }

        // -- Clip. Apply last so the clip outline tracks the content
        //    rather than the post-translate / post-rotate bounds.
        if (clipShape is not null)
        {
            modifier = modifier.Clip(clipShape);
        }

        return modifier;
    }

    /// <summary>
    /// Map a MAUI <see cref="IShape"/> to a Compose
    /// <see cref="ComposeShape"/>. Only the simple primitive
    /// geometries are recognised; unknown shapes / arbitrary paths
    /// return <see langword="null"/> so the caller skips the
    /// <c>Modifier.Clip</c> chain rather than emitting a wrong outline.
    /// </summary>
    static ComposeShape? TranslateClipToShape(IShape clip) => clip switch
    {
        // RoundRectangleGeometry — uniform CornerRadius is the common
        // XAML shape; MAUI's CornerRadius is a struct of four doubles.
        // Compose RoundedCornerShape supports four-corner builds via
        // RoundedCorners(topStart, topEnd, bottomEnd, bottomStart).
        RoundRectangleGeometry r => BuildRoundedRectangle(r),

        // RectangleGeometry — pure rectangle, no clipping outline
        // change. Compose's singleton RectangleShape is the canonical
        // "draw a rectangle, no rounding" answer.
        RectangleGeometry => ComposeShape.Rectangle,

        // EllipseGeometry — only the equal-radii (circle / pill) case
        // maps cleanly; an axis-aligned ellipse would require a
        // RoundedCornerShape with percent=50 and an aspect-aware
        // clip, which Compose doesn't expose as a single shape.
        EllipseGeometry e when Math.Abs(e.RadiusX - e.RadiusY) < 1e-6 => ComposeShape.Circle(),

        // Anything else (PathGeometry / GeometryGroup / line /
        // polyline / Shape subclasses with custom paths) falls
        // through to no-op clipping. We surface the documented
        // limitation in docs/maui-backend.md rather than emit a
        // wrong outline.
        _ => null,
    };

    /// <summary>
    /// Build a Compose <see cref="RoundedCornerShape"/> from a MAUI
    /// <see cref="RoundRectangleGeometry"/>. The rectangle bounds
    /// themselves are ignored (Compose's clip outline is implicitly
    /// the layout box), only the <c>CornerRadius</c> contributes.
    /// </summary>
    static ComposeShape BuildRoundedRectangle(RoundRectangleGeometry geo)
    {
        var corner = geo.CornerRadius;
        // Common case: a uniform corner radius — emit the cheap
        // single-Dp ctor so the JNI bridge doesn't have to build a
        // four-corner CornerSize array.
        if (Math.Abs(corner.TopLeft - corner.TopRight) < 1e-6
            && Math.Abs(corner.TopLeft - corner.BottomRight) < 1e-6
            && Math.Abs(corner.TopLeft - corner.BottomLeft) < 1e-6)
        {
            return new RoundedCornerShape(new Dp((float)corner.TopLeft));
        }

        // Per-corner radii: MAUI orders TopLeft / TopRight / BottomRight /
        // BottomLeft (visually clockwise from top-leading); Compose
        // uses topStart / topEnd / bottomEnd / bottomStart, which is
        // the same order under LTR layout (Start == Left). RTL apps
        // would mirror the start/end in Compose; MAUI's
        // RoundRectangleGeometry doesn't model that distinction so we
        // pass through verbatim and accept the LTR-only fidelity.
        return new RoundedCornerShape(
            topStart:    new Dp((float)corner.TopLeft),
            topEnd:      new Dp((float)corner.TopRight),
            bottomEnd:   new Dp((float)corner.BottomRight),
            bottomStart: new Dp((float)corner.BottomLeft));
    }
}
