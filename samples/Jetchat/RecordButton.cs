using System;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// C# port of upstream's <c>RecordButton.kt</c> + <c>RecordingIndicator</c>.
/// Renders the mic <see cref="IconButton"/> that replaces the "Send" button
/// when the text field is empty, plus the recording overlay (pulsing red
/// dot + MM:SS timer + slide-to-cancel affordance) that
/// <see cref="BuildRecordingIndicator"/> swaps into the
/// <see cref="TextField"/>'s slot via
/// <see cref="AnimatedContent{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Gesture parity gap.</strong> Upstream uses
/// <c>detectDragGesturesAfterLongPress</c> so press-and-hold starts
/// recording, releasing without swiping commits, and dragging left past a
/// threshold cancels. The compose-net facade layer doesn't surface
/// <c>detectDragGesturesAfterLongPress</c> yet (only the simpler
/// <see cref="Modifier.DetectTapGestures"/> and
/// <see cref="Modifier.Draggable(DraggableState, Orientation, bool)"/> are
/// bound), and the existing <c>onPress</c> press-gesture callback
/// intentionally doesn't expose the <c>awaitRelease</c> sub-scope. So this
/// port collapses the upstream gesture to:
/// </para>
/// <list type="bullet">
/// <item>Tap the mic when <em>not</em> recording → start recording.</item>
/// <item>Tap the mic while recording → finish ("release commits"
/// equivalent).</item>
/// <item>Swipe the mic left far enough while recording (via
/// <see cref="Modifier.Draggable(DraggableState, Orientation, bool)"/>) →
/// cancel.</item>
/// </list>
/// <para>
/// The pulsing red dot and MM:SS timer are driven manually by a
/// <see cref="LaunchedEffect"/> + <c>Task.Delay</c> loop because Compose's
/// <c>rememberInfiniteTransition</c> / <c>animateFloat</c> facades aren't
/// surfaced yet. Frame rate is intentionally capped at ~16 fps for the
/// pulse to keep main-thread work tiny — the visual effect is identical to
/// upstream's two-second tween for the relevant size/alpha range.
/// </para>
/// </remarks>
public static class RecordButton
{
    const float SwipeToCancelThresholdPx = 200f * 3f; // ~200dp at ~3x density
    const int   PulseFrameDelayMs        = 64;        // ~16 fps
    const int   PulseDurationMs          = 2000;

    /// <summary>
    /// Build the mic <see cref="IconButton"/>. When <paramref name="isRecording"/>
    /// is true the icon grows, gains a circular background, and starts
    /// dispatching drag deltas to <paramref name="swipeOffset"/>. When the
    /// drag offset crosses <see cref="SwipeToCancelThresholdPx"/> to the
    /// left, <paramref name="onCancel"/> fires. Tapping always toggles
    /// recording — tap to start, tap to finish.
    /// </summary>
    /// <param name="isRecording">Two-way state: true while a recording is
    /// in progress.</param>
    /// <param name="swipeOffset">Two-way horizontal-offset accumulator,
    /// in pixels. Reset to <c>0</c> on recording start / cancel / finish.
    /// Read by <see cref="BuildRecordingIndicator"/> to slide the
    /// "Slide to cancel" hint and fade it out as the user swipes.</param>
    /// <param name="onCommit">Called when the user taps the mic while
    /// recording — the "release commits" hook.</param>
    /// <param name="onCancel">Called when the user swipes left past the
    /// cancel threshold.</param>
    /// <param name="scheme">Active <see cref="ColorScheme"/> used for tint
    /// + background when recording.</param>
    public static ComposableNode BuildButton(
        MutableState<bool>          isRecording,
        MutableNumberState<float>   swipeOffset,
        Action                      onCommit,
        Action                      onCancel,
        ColorScheme                 scheme)
    {
        bool recording = isRecording.Value;
        var dragState  = Compose.RememberDraggableState(delta =>
        {
            if (!isRecording.Value) return;
            swipeOffset.Value += delta;
            if (swipeOffset.Value <= -SwipeToCancelThresholdPx)
                onCancel();
        });

        var modifier = Modifier.Companion;
        if (recording)
        {
            modifier = modifier
                .Size(56)
                .Background(Color.Red, Shape.RoundedCorners(28, 28, 28, 28))
                .Draggable(dragState, Orientation.Horizontal);
        }

        var iconButton = new IconButton(onClick: () =>
        {
            if (isRecording.Value)
                onCommit();
            else
                isRecording.Value = true;
        })
        {
            new Icon(Resource.Drawable.ic_mic, "Record voice message")
            {
                TintArgb = recording ? scheme.OnPrimary : scheme.OnSurfaceVariant,
            },
        };
        iconButton.Modifier = modifier;
        return iconButton;
    }

    /// <summary>
    /// Build the recording overlay row that replaces the
    /// <see cref="TextField"/> while recording is active.
    /// Layout: <c>[ pulsing red dot ] [ MM:SS timer ] [ ← Slide to cancel ]</c>.
    /// The slide hint translates by <c>swipeOffset / 2</c> and fades out
    /// as the offset approaches the cancel threshold, matching upstream's
    /// <c>graphicsLayer { translationX = swipeOffset() / 2; alpha = 1 - abs(swipeOffset()) / swipeThreshold }</c>.
    /// </summary>
    /// <param name="swipeOffset">Two-way horizontal-offset accumulator,
    /// in pixels — owned by the parent so the swipe value survives the
    /// <see cref="AnimatedContent{T}"/> swap.</param>
    /// <param name="scheme">Active <see cref="ColorScheme"/> used for the
    /// timer and slide-to-cancel hint colors.</param>
    public static ComposableNode BuildRecordingIndicator(
        MutableNumberState<float> swipeOffset,
        ColorScheme               scheme)
    {
        var pulse   = Compose.Remember(() => new MutableNumberState<float>(1f));
        var seconds = Compose.Remember(() => new MutableNumberState<int>(0));

        return new Composed(c =>
        {
            // 1-second tick timer: bumps `seconds` for the MM:SS label.
            // Owns its own LaunchedEffect so it cancels with this subtree
            // when AnimatedContent swaps the indicator out.
            // Manual pulse animation (replaces upstream's
            // infiniteRepeatable(tween(2000), RepeatMode.Reverse)).
            // We update `pulse` every PulseFrameDelayMs by linearly
            // sweeping a phase through [0, 2π) — sin gives a smooth
            // 1f ↔ 0.2f modulation with the same 2-second period as
            // upstream.

            float offset    = swipeOffset.Value;
            float alphaHint = System.MathF.Max(0f, 1f - System.MathF.Abs(offset) / SwipeToCancelThresholdPx);
            int   mins      = seconds.Value / 60;
            int   secs      = seconds.Value % 60;
            string timer    = $"{mins:D2}:{secs:D2}";
            float pulseValue = pulse.Value;

            return new Row
            {
                Modifier.Companion.FillMaxSize(),

                new LaunchedEffect(key1: "recording-timer", async ct =>
                {
                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            await System.Threading.Tasks.Task.Delay(1000, ct);
                            seconds.Value++;
                        }
                    }
                    catch (OperationCanceledException) { }
                }),

                new LaunchedEffect(key1: "recording-pulse", async ct =>
                {
                    try
                    {
                        long startMs = System.Environment.TickCount64;
                        while (!ct.IsCancellationRequested)
                        {
                            long ms = System.Environment.TickCount64 - startMs;
                            // Triangle wave over [0..1] with period = PulseDurationMs.
                            // 1.0 → 0.2 → 1.0 every two seconds.
                            float phase = (ms % PulseDurationMs) / (float)PulseDurationMs;
                            float tri = phase < 0.5f ? (1f - phase * 2f) : ((phase - 0.5f) * 2f);
                            pulse.Value = 0.2f + tri * 0.8f;
                            await System.Threading.Tasks.Task.Delay(PulseFrameDelayMs, ct);
                        }
                    }
                    catch (OperationCanceledException) { }
                }),

                new Spacer(Modifier.Companion.Width(16)),

                new Box
                {
                    Modifier.Companion
                        .Size(16)
                        .Scale(pulseValue)
                        .Background(Color.Red, Shape.RoundedCorners(8, 8, 8, 8)),
                },

                new Spacer(Modifier.Companion.Width(12)),

                new Text(timer)
                {
                    FontSize   = 14,
                    FontWeight = FontWeight.Medium,
                    Color      = new Color(scheme.OnSurface),
                },

                new Spacer(Modifier.Companion.Width(16)),

                new Row
                {
                    Modifier.Companion
                        .Weight(1f, fill: true)
                        .Offset(x: offset / 2f / 3f)
                        .Alpha(alphaHint),

                    new Icon(Resource.Drawable.ic_arrow_back, "Slide to cancel")
                    {
                        TintArgb = scheme.OnSurfaceVariant,
                        Modifier = Modifier.Companion.Size(16),
                    },
                    new Spacer(Modifier.Companion.Width(4)),
                    new Text("Slide to cancel")
                    {
                        FontSize = 14,
                        Color    = new Color(scheme.OnSurfaceVariant),
                    },
                },
            };
        });
    }
}
