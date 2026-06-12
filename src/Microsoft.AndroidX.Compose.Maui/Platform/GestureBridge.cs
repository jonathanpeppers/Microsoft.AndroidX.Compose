using System.Runtime.CompilerServices;
using AndroidX.Compose;
using IPanGestureController = Microsoft.Maui.Controls.IPanGestureController;
using IPinchGestureController = Microsoft.Maui.Controls.IPinchGestureController;
using MauiButtonsMask = Microsoft.Maui.Controls.ButtonsMask;
using MauiPanRecognizer = Microsoft.Maui.Controls.PanGestureRecognizer;
using MauiPinchRecognizer = Microsoft.Maui.Controls.PinchGestureRecognizer;
using MauiPointerRecognizer = Microsoft.Maui.Controls.PointerGestureRecognizer;
using MauiSwipeDirection = Microsoft.Maui.SwipeDirection;
using MauiSwipeRecognizer = Microsoft.Maui.Controls.SwipeGestureRecognizer;
using MauiTapRecognizer = Microsoft.Maui.Controls.TapGestureRecognizer;
using MauiView = Microsoft.Maui.Controls.View;

namespace Microsoft.AndroidX.Compose.Maui.Platform;

/// <summary>
/// Translates a MAUI cross-platform <see cref="IView"/>'s declarative
/// <c>GestureRecognizers</c> collection into Compose
/// <c>Modifier.PointerInput(key) { detect*Gestures(...) }</c> chains
/// so taps / pans / pinches / swipes / pointer-events fire when the
/// view is folded into a Compose composition (the common path after
/// Phase 2 Slice 2 — see <see cref="ComposeWalker"/>).
/// </summary>
/// <remarks>
/// <para><b>Why this is needed.</b> MAUI's stock
/// <c>GesturePlatformManager</c> wires <c>handler.ToPlatform().Touch</c>
/// in its constructor; for our Compose-folded leaves the leaf
/// <c>ComposeView</c> is detached (only <see cref="Handlers.PageHandler"/>'s
/// page-rooted <c>ComposeView</c> is attached) so the platform-side
/// touch event never fires. Touch routing for folded content goes
/// through Compose's <c>pointerInput</c> system on the page-rooted
/// view, so we re-implement the gesture-recognition layer there.</para>
///
/// <para><b>Mapping table.</b></para>
/// <list type="table">
///   <listheader><term>Recognizer</term><description>Compose detector + fire API</description></listheader>
///   <item><term><see cref="MauiTapRecognizer"/></term>
///     <description><c>detectTapGestures(onTap, onDoubleTap)</c> →
///     <c>SendTapped(view)</c> on tap (or double-tap when
///     <c>NumberOfTapsRequired == 2</c>).</description></item>
///   <item><term><see cref="MauiPanRecognizer"/></term>
///     <description><c>detectDragGestures(onDragStart, onDrag, onDragEnd, onDragCancel)</c>
///     → <c>SendPanStarted</c>, accumulated total <c>SendPan</c>, then
///     <c>SendPanCompleted</c> / <c>SendPanCanceled</c>. Each gesture
///     gets a per-bridge-instance gesture id.</description></item>
///   <item><term><see cref="MauiPinchRecognizer"/></term>
///     <description><c>detectTransformGestures(onGesture)</c> →
///     <c>SendPinchStarted</c> on first call, <c>SendPinch</c> with
///     per-frame zoom, <c>SendPinchEnded</c> when no longer pinching
///     (signal: zoom returns to 1.0 for several frames). v1 ends the
///     gesture only on view detach; the lack of explicit
///     end-of-gesture from <c>detectTransformGestures</c> is the
///     deliberate Compose API choice.</description></item>
///   <item><term><see cref="MauiSwipeRecognizer"/></term>
///     <description><c>detectDragGestures(onDrag, onDragEnd)</c> with
///     a synthesized direction at end-of-drag based on the dominant
///     axis and <see cref="MauiSwipeRecognizer.Threshold"/> (default
///     100 dp).</description></item>
///   <item><term><see cref="MauiPointerRecognizer"/></term>
///     <description><c>detectTapGestures(onPress, onTap)</c> →
///     <c>SendPointerPressed</c> + <c>SendPointerReleased</c>. Move /
///     Enter / Exit are mouse-only on Android; deferred until a
///     hover-aware detector ships.</description></item>
/// </list>
///
/// <para><b>Modifier.PointerInput key strategy.</b> Compose's
/// <c>pointerInput(key)</c> only restarts its gesture-detector
/// coroutine when the key changes. We compute a stable per-recognizer-
/// list hash from the recognizer types and instance identities. The
/// key is a <see cref="Java.Lang.Integer"/> wrapping that hash;
/// Compose's slot-table comparison uses
/// <c>Java.Lang.Integer.equals</c>, so the same hash across two
/// recompositions does NOT restart the coroutine, but adding /
/// removing a recognizer at runtime DOES (different instance set →
/// different hash → key changes → coroutine restarts with the new
/// callbacks). This avoids the obvious anti-pattern of keying on
/// <c>view</c> itself, which pins the coroutine to the first
/// composition's callback closure forever.</para>
///
/// <para><b>Runtime add/remove of recognizers.</b> The bridge re-runs
/// only when the handler's view-properties version slot bumps (any
/// <see cref="IView"/> property mapper change re-runs <c>BuildNode</c>
/// → <c>ApplyGestures</c>). Mutating <c>view.GestureRecognizers</c>
/// at runtime without also touching another mapped property won't
/// pick up the new set until the next recomposition. Acceptable for
/// v1; subscribing to <c>INotifyCollectionChanged</c> on the
/// recognizer list is the obvious extension.</para>
///
/// <para><b>Multi-pointer gesture id correlation.</b>
/// <see cref="MauiPanRecognizer"/>'s
/// <c>IPanGestureController.SendPan(...)</c> takes a stable
/// <c>gestureId</c> for the lifecycle <c>SendPanStarted →
/// SendPan(...)*+ → SendPanCompleted/Canceled</c>. We allocate one id
/// per drag (incremented from a process-wide
/// <see cref="MauiPanRecognizer.CurrentId"/> mirror so the id is a
/// fresh integer that won't collide with an unrelated ongoing gesture
/// on another bridge). The id flows through the closure captured by
/// <see cref="Modifier.DetectDragGestures(Modifier, Action{Offset}, Action{Offset}?, Action?, Action?, object?)"/>.</para>
///
/// <para><b>Trade-off vs Compose's own gesture detectors on
/// <c>Button</c> / <c>Slider</c>.</b> Compose composables that own
/// their own gesture handling (<c>Button</c> calls
/// <c>Modifier.clickable</c> internally; <c>Slider</c> calls
/// <c>Modifier.draggable</c>) already win for in-composition
/// gestures: their <c>pointerInput</c> ops sit closer to the leaf in
/// the modifier chain, so MAUI's outer
/// <see cref="ApplyGestures(Modifier, IView, IMauiContext)"/> chain
/// only fires for events the inner detectors don't consume. This
/// matches the user's intuition: a <c>TapGestureRecognizer</c> on a
/// <see cref="Microsoft.Maui.Controls.Button"/> is redundant with
/// <see cref="Microsoft.Maui.Controls.Button.Clicked"/>; Compose's
/// own handler captures the press first. The bridge wins for
/// declarative <c>GestureRecognizers</c> on non-interactive views
/// (<see cref="Microsoft.Maui.Controls.Label"/>,
/// <see cref="Microsoft.Maui.Controls.BoxView"/>, etc.) where
/// Compose's leaf composable doesn't install its own
/// <c>pointerInput</c>.</para>
/// </remarks>
internal static class GestureBridge
{
    /// <summary>
    /// Layer Compose <c>pointerInput</c> modifiers onto
    /// <paramref name="modifier"/> for every entry in
    /// <paramref name="view"/>'s <c>GestureRecognizers</c> (when
    /// <paramref name="view"/> is a <see cref="MauiView"/> — pure
    /// <see cref="IView"/> implementations don't carry recognizers).
    /// Returns <paramref name="modifier"/> unchanged when the view
    /// has no recognizers — no allocations, no JNI calls in the
    /// common case.
    /// </summary>
    /// <param name="modifier">Modifier to chain onto, typically the
    /// result of <see cref="ModifierBridge.ApplyViewProperties"/>.</param>
    /// <param name="view">The MAUI virtual view; only
    /// <see cref="MauiView"/> subclasses carry recognizers.</param>
    /// <param name="mauiContext">Reserved for future use (e.g.
    /// dispatching commands on the UI thread); ignored in v1.</param>
    /// <returns>A modifier with one
    /// <c>Modifier.PointerInput(key) { detect*Gestures(...) }</c> per
    /// recognizer, in declaration order.</returns>
    internal static Modifier ApplyGestures(
        this Modifier modifier,
        IView view,
        IMauiContext? mauiContext)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        ArgumentNullException.ThrowIfNull(view);
        _ = mauiContext;

        if (view is not MauiView controlsView) return modifier;

        var recognizers = controlsView.GestureRecognizers;
        if (recognizers is null || recognizers.Count == 0) return modifier;

        // Stable composite key. The hash combines the count + each
        // recognizer's runtime identity hash so add/remove changes
        // it but no-op recompositions don't.
        int hash = 17 ^ recognizers.Count;
        foreach (var r in recognizers)
        {
            hash = unchecked((hash * 31) ^ RuntimeHelpers.GetHashCode(r));
        }
        var key = Java.Lang.Integer.ValueOf(hash);

        foreach (var r in recognizers)
        {
            switch (r)
            {
                case MauiTapRecognizer tap:
                    modifier = ApplyTap(modifier, tap, controlsView, key);
                    break;
                case MauiPanRecognizer pan:
                    modifier = ApplyPan(modifier, pan, controlsView, key);
                    break;
                case MauiPinchRecognizer pinch:
                    modifier = ApplyPinch(modifier, pinch, controlsView, key);
                    break;
                case MauiSwipeRecognizer swipe:
                    modifier = ApplySwipe(modifier, swipe, controlsView, key);
                    break;
                case MauiPointerRecognizer pointer:
                    modifier = ApplyPointer(modifier, pointer, controlsView, key);
                    break;
                // Other recognizers (DragGestureRecognizer / DropGestureRecognizer)
                // route through MAUI's separate drag-and-drop pipeline,
                // already covered by Modifier.DragAndDropTarget in
                // Slice 7. No-op here.
            }
        }

        return modifier;
    }

    // -- Tap ----------------------------------------------------------

    static Modifier ApplyTap(Modifier modifier, MauiTapRecognizer tap, MauiView view, Java.Lang.Integer key)
    {
        // NumberOfTapsRequired == 2 routes through onDoubleTap;
        // otherwise onTap. Single + double on the same view requires
        // two separate recognizers (matching MAUI's per-recognizer
        // semantics).
        if (tap.NumberOfTapsRequired >= 2)
        {
            return modifier.DetectTapGestures(
                onDoubleTap: _ => SendTapped(tap, view, null),
                key:         key);
        }

        return modifier.DetectTapGestures(
            onTap: _ => SendTapped(tap, view, null),
            key:   key);
    }

    // -- Pan ----------------------------------------------------------

    static Modifier ApplyPan(Modifier modifier, MauiPanRecognizer pan, MauiView view, Java.Lang.Integer key)
    {
        var density = global::Android.Content.Res.Resources.System!.DisplayMetrics!.Density;
        var controller = (IPanGestureController)pan;

        // Per-bridge-instance state. Captured into the closures and
        // reset by onDragStart so a new gesture starts at (0, 0) total.
        double totalX = 0d, totalY = 0d;
        int gestureId = 0;

        return modifier.DetectDragGestures(
            onDragStart: _ =>
            {
                totalX = 0d;
                totalY = 0d;
                gestureId = AllocateGestureId();
                controller.SendPanStarted(view, gestureId);
            },
            onDrag: delta =>
            {
                // detectDragGestures hands us per-frame deltas in
                // pixels; SendPan expects cumulative totals in
                // density-independent pixels.
                totalX += delta.X / density;
                totalY += delta.Y / density;
                controller.SendPan(view, totalX, totalY, gestureId);
            },
            onDragEnd:    () => controller.SendPanCompleted(view, gestureId),
            onDragCancel: () => controller.SendPanCanceled(view, gestureId),
            key:          key);
    }

    static int s_panGestureCounter;
    static int AllocateGestureId() => Interlocked.Increment(ref s_panGestureCounter);

    // -- Pinch --------------------------------------------------------

    static Modifier ApplyPinch(Modifier modifier, MauiPinchRecognizer pinch, MauiView view, Java.Lang.Integer key)
    {
        var controller = (IPinchGestureController)pinch;

        // Compose's detectTransformGestures fires per-frame zoom
        // multipliers but offers no start / end events. We latch a
        // "started" flag on first non-1.0 zoom and let the next view-
        // detach / key-change emit the implicit end (deferred). v1
        // surfaces just SendPinch(scaleDelta, currentScalePoint).
        bool started = false;

        return modifier.DetectTransformGestures(
            onGesture: (centroid, _, zoom, _) =>
            {
                if (!started)
                {
                    controller.SendPinchStarted(view, new Point(centroid.X, centroid.Y));
                    started = true;
                }
                controller.SendPinch(view, zoom, new Point(centroid.X, centroid.Y));
            },
            key: key);
    }

    // -- Swipe --------------------------------------------------------

    static Modifier ApplySwipe(Modifier modifier, MauiSwipeRecognizer swipe, MauiView view, Java.Lang.Integer key)
    {
        var density = global::Android.Content.Res.Resources.System!.DisplayMetrics!.Density;

        // Accumulate per-frame deltas, then synthesize a direction at
        // onDragEnd if the dominant-axis total exceeds the threshold.
        double totalX = 0d, totalY = 0d;

        return modifier.DetectDragGestures(
            onDragStart: _ => { totalX = 0d; totalY = 0d; },
            onDrag: delta =>
            {
                totalX += delta.X / density;
                totalY += delta.Y / density;
            },
            onDragEnd: () =>
            {
                // Threshold defaults to 100 dp.
                double threshold = swipe.Threshold;
                MauiSwipeDirection? direction = null;
                if (Math.Abs(totalX) >= Math.Abs(totalY) && Math.Abs(totalX) >= threshold)
                {
                    direction = totalX > 0 ? MauiSwipeDirection.Right : MauiSwipeDirection.Left;
                }
                else if (Math.Abs(totalY) >= threshold)
                {
                    direction = totalY > 0 ? MauiSwipeDirection.Down : MauiSwipeDirection.Up;
                }

                if (direction is { } dir && (swipe.Direction == 0 || (swipe.Direction & dir) != 0))
                {
                    swipe.SendSwiped(view, dir);
                }
            },
            key: key);
    }

    // -- Pointer ------------------------------------------------------

    static Modifier ApplyPointer(Modifier modifier, MauiPointerRecognizer pointer, MauiView view, Java.Lang.Integer key)
    {
        // Touch hardware on Android only fires
        // ACTION_DOWN/MOVE/UP/CANCEL — there's no hover. Map Pressed
        // to onPress (which fires synchronously at finger-down) and
        // Released to onTap (which fires after the tap-slop check
        // resolves). Move / Enter / Exit are mouse-only and aren't
        // exposed by detectTapGestures; deferred until a hover-aware
        // detector is bridged.
        return modifier.DetectTapGestures(
            onPress: _ => SendPointerPressed(pointer, view, null, null, MauiButtonsMask.Primary),
            onTap:   _ => SendPointerReleased(pointer, view, null, null, MauiButtonsMask.Primary),
            key:     key);
    }

    // -- UnsafeAccessor shims -----------------------------------------

    // TapGestureRecognizer.SendTapped is internal in MAUI 10.0.20.
    // The corresponding fire method on PointerGestureRecognizer is
    // also internal. UnsafeAccessor is the IL-trim-safe way to call
    // them without reflection or InternalsVisibleTo cooperation.

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SendTapped")]
    static extern void SendTapped(MauiTapRecognizer instance, MauiView sender, Func<IElement?, Point?>? getPosition);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SendPointerPressed")]
    static extern void SendPointerPressed(
        MauiPointerRecognizer instance, MauiView sender, Func<IElement?, Point?>? getPosition,
        PlatformPointerEventArgs? platformArgs, MauiButtonsMask button);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "SendPointerReleased")]
    static extern void SendPointerReleased(
        MauiPointerRecognizer instance, MauiView sender, Func<IElement?, Point?>? getPosition,
        PlatformPointerEventArgs? platformArgs, MauiButtonsMask button);
}
