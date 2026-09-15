namespace AndroidX.Compose.Samples.Jetchat;

// Pinned voiceRecordingGesture: cancellation is a left swipe inside the vertical corridor.
internal sealed class RecordingGestureState
{
    internal float Horizontal { get; private set; }
    internal float Vertical { get; private set; }
    internal bool Dragging { get; private set; }

    internal void Start()
    {
        Horizontal = Vertical = 0;
        Dragging = true;
    }

    internal bool Move(float x, float y, float density)
    {
        if (!Dragging)
            return false;
        Horizontal += x;
        Vertical += y;
        if (Horizontal < 0 && System.MathF.Abs(Horizontal) >= 200 * density && System.MathF.Abs(Vertical) <= 80 * density)
        {
            Dragging = false;
            return true;
        }
        return false;
    }

    internal bool End()
    {
        bool finish = Dragging;
        Dragging = false;
        return finish;
    }
}
