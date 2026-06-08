using System;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// C# port of upstream's <c>RecordButton.kt</c> + <c>RecordingIndicator</c>.
/// See <c>samples/Jetchat/README.md</c> for the gesture and animation
/// parity gaps in this port.
/// </summary>
public static class RecordButton
{
    const int SwipeToCancelThresholdDp = 200;
    const int PulseFrameDelayMs        = 64;
    const int PulseDurationMs          = 2000;

    static float SwipeToCancelThresholdPx =>
        SwipeToCancelThresholdDp * Android.Content.Res.Resources.System!.DisplayMetrics!.Density;

    /// <summary>Build the mic <see cref="IconButton"/>.</summary>
    public static ComposableNode BuildButton(
        MutableState<bool>          isRecording,
        MutableNumberState<float>   swipeOffset,
        Action                      onCommit,
        Action                      onCancel,
        ColorScheme                 scheme)
    {
        bool recording = isRecording.Value;
        float threshold = SwipeToCancelThresholdPx;
        var dragState  = Compose.RememberDraggableState(delta =>
        {
            if (!isRecording.Value) return;
            swipeOffset.Value += delta;
            if (swipeOffset.Value <= -threshold)
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

    /// <summary>Build the recording overlay row that replaces the
    /// <see cref="TextField"/> while recording is active.</summary>
    public static ComposableNode BuildRecordingIndicator(
        MutableNumberState<float> swipeOffset,
        ColorScheme               scheme)
    {
        var pulse   = Compose.Remember(() => new MutableNumberState<float>(1f));
        var seconds = Compose.Remember(() => new MutableNumberState<int>(0));

        return new Composed(c =>
        {
            float density   = Android.Content.Res.Resources.System!.DisplayMetrics!.Density;
            float threshold = SwipeToCancelThresholdDp * density;
            float offset    = swipeOffset.Value;
            float alphaHint = System.MathF.Max(0f, 1f - System.MathF.Abs(offset) / threshold);
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
                        .Offset(x: offset / 2f / density)
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
