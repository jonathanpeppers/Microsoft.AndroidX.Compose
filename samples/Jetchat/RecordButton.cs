using AndroidX.Compose.Animation.Core;
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

    /// <summary>Build the mic button with theme-derived icon tint and an animated recording background.</summary>
    /// <remarks>The enclosing tooltip owns parent-layout alignment; this anchor recomposes independently.</remarks>
    public static ComposableNode BuildButton(
        MutableState<bool>          isRecording,
        MutableNumberState<float>   swipeOffset,
        Action                      onCommit,
        Action                      onCancel,
        ColorScheme                 scheme) =>
        new Composed(c =>
        {
            bool recording = isRecording.Value;
            float density = SwipeToCancelThresholdPx / SwipeToCancelThresholdDp;
            var gesture = c.Remember(() => new RecordingGestureState());

            var visuals = RecordButtonVisuals.Read(c, recording);
            var innerModifier = Modifier.FillMaxSize().Padding(18);

            return new Box
            {
                Modifier
                    .Size(56)
                    .DetectDragGesturesAfterLongPress(
                        onDragStart: _ =>
                        {
                            gesture.Start();
                            swipeOffset.Value = 0;
                            isRecording.Value = true;
                        },
                        onDrag: delta =>
                        {
                            if (!gesture.Dragging) return;
                            bool cancelled = gesture.Move(delta.X, delta.Y, density);
                            swipeOffset.Value = gesture.Horizontal;
                            if (cancelled) onCancel();
                        },
                        onDragEnd: () =>
                        {
                            if (gesture.End()) onCommit();
                        },
                        onDragCancel: () =>
                        {
                            if (gesture.End()) onCancel();
                        }),
                visuals.Background,
                new Box
                {
                    innerModifier,
                    new Icon(Resource.Drawable.ic_mic, "Record voice message")
                    {
                        Tint = visuals.IconColor,
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
        BuildRecordingIndicator(swipeOffset, scheme, null);

    internal static ComposableNode BuildRecordingIndicator(
        MutableNumberState<float> swipeOffset,
        ColorScheme               scheme,
        Action<float>?            pulseObserver) =>
        new Composed(c =>
        {
            var seconds = c.MutableStateOf(0);
            var pulseTransition = c.RememberInfiniteTransition("recording-pulse");
            var pulseSpec = c.Remember(() => AnimationSpecs.InfiniteRepeatable(
                AnimationSpecs.Tween(2000), RepeatMode.Reverse));
            var pulse = pulseTransition.AnimateFloat(c, 1f, 0.2f, pulseSpec, "recording-pulse-scale");

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
            if (pulseObserver is not null)
                c.SideEffect(() => pulseObserver(pulseValue));

            return new Row(
                horizontalArrangement: null,
                verticalAlignment: Alignment.Vertical.CenterVertically)
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
                    Modifier   = Modifier.AlignByBaseline(),
                    Color      = Color.FromPacked(scheme.OnSurface),
                },

                new Box
                {
                    Modifier
                        .AlignByBaseline()
                        .Weight(1f, fill: true)
                        .FillMaxSize()
                        .ClipToBounds(),
                    new Text("Swipe to cancel")
                    {
                        Modifier = Modifier
                            .Align(Alignment.Center)
                            .Offset(x: offset / 2f / density)
                            .Alpha(alphaHint),
                        FontSize = 16,
                        Color    = Color.FromPacked(scheme.OnSurfaceVariant),
                    },
                },
            };
        });
}
