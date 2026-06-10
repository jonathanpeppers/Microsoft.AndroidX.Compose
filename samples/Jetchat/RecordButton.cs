using AndroidX.Compose.Material3;

namespace AndroidX.Compose.Samples.Jetchat;

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

    /// <summary>Build the mic button (idle gray icon, or red recording pill).</summary>
    public static ComposableNode BuildButton(
        MutableState<bool>          isRecording,
        MutableNumberState<float>   swipeOffset,
        Action                      onCommit,
        Action                      onCancel,
        ColorScheme                 scheme) =>
        new Composed(c =>
        {
            bool recording = isRecording.Value;
            float threshold = SwipeToCancelThresholdPx;
            var dragState  = c.RememberDraggableState(delta =>
            {
                if (!isRecording.Value) return;
                swipeOffset.Value += delta;
                if (swipeOffset.Value <= -threshold)
                    onCancel();
            });

            Action onClick = () =>
            {
                if (isRecording.Value)
                    onCommit();
                else
                    isRecording.Value = true;
            };

            var innerModifier = Modifier.Companion.FillMaxSize();
            if (recording)
                innerModifier = innerModifier
                    .Background(Color.Red, Shape.RoundedCorners(28, 28, 28, 28))
                    .Draggable(dragState, Orientation.Horizontal);
            innerModifier = innerModifier.Padding(16);

            return new Box
            {
                Modifier.Companion
                    .Align(Alignment.Vertical.CenterVertically)
                    .Size(56)
                    .Clickable(onClick),
                new Box
                {
                    innerModifier,
                    new Icon(Resource.Drawable.ic_mic, "Record voice message")
                    {
                        TintArgb = recording ? scheme.OnPrimary : scheme.OnSurfaceVariant,
                        Modifier = Modifier.Companion.FillMaxSize(),
                    },
                },
            };
        });

    /// <summary>Build the recording overlay row that replaces the
    /// <see cref="TextField"/> while recording is active.</summary>
    public static ComposableNode BuildRecordingIndicator(
        MutableNumberState<float> swipeOffset,
        ColorScheme               scheme) =>
        new Composed(c =>
        {
            var pulse   = c.Remember(() => new MutableNumberState<float>(1f));
            var seconds = c.Remember(() => new MutableNumberState<int>(0));

            float density   = Android.Content.Res.Resources.System!.DisplayMetrics!.Density;
            float threshold = SwipeToCancelThresholdDp * density;
            float offset    = swipeOffset.Value;
            float alphaHint = MathF.Max(0f, 1f - MathF.Abs(offset) / threshold);
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
                            await Task.Delay(1000, ct);
                            seconds.Value++;
                        }
                    }
                    catch (OperationCanceledException) { }
                }),

                new LaunchedEffect(key1: "recording-pulse", async ct =>
                {
                    try
                    {
                        long startMs = Environment.TickCount64;
                        while (!ct.IsCancellationRequested)
                        {
                            long ms = Environment.TickCount64 - startMs;
                            float phase = (ms % PulseDurationMs) / (float)PulseDurationMs;
                            float tri = phase < 0.5f ? (1f - phase * 2f) : ((phase - 0.5f) * 2f);
                            pulse.Value = 0.2f + tri * 0.8f;
                            await Task.Delay(PulseFrameDelayMs, ct);
                        }
                    }
                    catch (OperationCanceledException) { }
                }),

                new Box
                {
                    Modifier.Companion
                        .Align(Alignment.Vertical.CenterVertically)
                        .Size(56)
                        .Padding(24)
                        .Scale(pulseValue)
                        .Background(Color.Red, Shape.RoundedCorners(28, 28, 28, 28)),
                },

                new Text(timer)
                {
                    Modifier   = Modifier.Companion.Align(Alignment.Vertical.CenterVertically),
                    FontSize   = 22,
                    FontWeight = FontWeight.Medium,
                    Color      = new Color(scheme.OnSurface),
                },

                new Spacer(Modifier.Companion.Width(16)),

                new Row
                {
                    Modifier.Companion
                        .Align(Alignment.Vertical.CenterVertically)
                        .Weight(1f, fill: true)
                        .Offset(x: offset / 2f / density)
                        .Alpha(alphaHint),

                    new Icon(Resource.Drawable.ic_arrow_back, "Slide to cancel")
                    {
                        TintArgb = scheme.OnSurfaceVariant,
                        Modifier = Modifier.Companion.Align(Alignment.Vertical.CenterVertically).Size(24),
                    },
                    new Spacer(Modifier.Companion.Width(8)),
                    new Text("Slide to cancel")
                    {
                        Modifier = Modifier.Companion.Align(Alignment.Vertical.CenterVertically),
                        FontSize = 16,
                        Color    = new Color(scheme.OnSurfaceVariant),
                    },
                },
            };
        });
}
