namespace Microsoft.AndroidX.Compose.DeviceTests;

/// <summary>Screen bounds of a slider, used to inject touch input on its track.</summary>
internal sealed record SliderTouchBounds(float Left, float Top, int Width, int Height)
{
    internal float CenterY => Top + Height / 2f;

    /// <summary>Screen x at <paramref name="fraction"/> of the slider's width.</summary>
    internal float X(float fraction) => Left + Width * fraction;
}
