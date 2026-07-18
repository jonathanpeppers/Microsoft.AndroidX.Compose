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

    static float SwipeToCancelThresholdPx
    {
        get
        {
            var resources = Android.Content.Res.Resources.System
                ?? throw new InvalidOperationException("Android system resources were unavailable in Jetchat.");
            var metrics = resources.DisplayMetrics
                ?? throw new InvalidOperationException("Android display metrics were unavailable in Jetchat.");
            return SwipeToCancelThresholdDp * metrics.Density;
        }
    }

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

            var innerModifier = Modifier.FillMaxSize();
            if (recording)
                innerModifier = innerModifier
                    .Background(Color.Red, new RoundedCornerShape(28.Dp()))
                    .Draggable(dragState, Orientation.Horizontal);
            innerModifier = innerModifier.Padding(16);

            return new Box
            {
                Modifier
                    .Align(Alignment.Vertical.CenterVertically)
                    .Size(56)
                    .Clickable(onClick),
                new Box
                {
                    innerModifier,
                    new Icon(Resource.Drawable.ic_mic, "Record voice message")
                    {
                        Tint = Color.FromPacked(
                            recording ? scheme.OnPrimary : scheme.OnSurfaceVariant),
                        Modifier = Modifier.FillMaxSize(),
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
            var pulse   = c.MutableStateOf(1f);
            var seconds = c.MutableStateOf(0);

            var resources = Android.Content.Res.Resources.System
                ?? throw new InvalidOperationException("Android system resources were unavailable in Jetchat.");
            var metrics = resources.DisplayMetrics
                ?? throw new InvalidOperationException("Android display metrics were unavailable in Jetchat.");
            float density   = metrics.Density;
            float threshold = SwipeToCancelThresholdDp * density;
            float offset    = swipeOffset.Value;
            float alphaHint = MathF.Max(0f, 1f - MathF.Abs(offset) / threshold);
            int   mins      = seconds.Value / 60;
            int   secs      = seconds.Value % 60;
            string timer    = $"{mins:D2}:{secs:D2}";
            float pulseValue = pulse.Value;

            return new Row
            {
                Modifier.FillMaxSize(),

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
                    Modifier
                        .Align(Alignment.Vertical.CenterVertically)
                        .Size(56)
                        .Padding(24)
                        .Scale(pulseValue)
                        .Background(Color.Red, new RoundedCornerShape(28.Dp())),
                },

                new Text(timer)
                {
                    Modifier   = Modifier.Align(Alignment.Vertical.CenterVertically),
                    FontSize   = 22,
                    FontWeight = FontWeight.Medium,
                    Color      = Color.FromPacked(scheme.OnSurface),
                },

                Spacer.Width(16),

                new Row
                {
                    Modifier
                        .Align(Alignment.Vertical.CenterVertically)
                        .Weight(1f, fill: true)
                        .Offset(x: offset / 2f / density)
                        .Alpha(alphaHint),

                    new Icon(Resource.Drawable.ic_arrow_back, "Swipe to cancel")
                    {
                        Tint = Color.FromPacked(scheme.OnSurfaceVariant),
                        Modifier = Modifier.Align(Alignment.Vertical.CenterVertically).Size(24),
                    },
                    Spacer.Width(8),
                    new Text("Swipe to cancel")
                    {
                        Modifier = Modifier.Align(Alignment.Vertical.CenterVertically),
                        FontSize = 16,
                        Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                    },
                },
            };
        });
}
