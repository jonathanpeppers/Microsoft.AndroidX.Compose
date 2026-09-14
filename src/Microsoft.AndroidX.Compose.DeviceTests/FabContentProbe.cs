using AndroidX.Compose;
using AndroidX.Compose.Material3;
using AndroidX.Compose.Runtime;
using Color = AndroidX.Compose.UI.Graphics.Color;
using Dp = AndroidX.Compose.UI.Unit.Dp;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class FabContentProbe(FabStylingTestActivity activity) : ComposableNode
{
    public override void Render(IComposer composer)
    {
        var identity = composer.Remember(static () => new object());
        var color = composer.Consume(ContentColorKt.LocalContentColor) as Color
            ?? throw new InvalidOperationException("FAB content did not receive a native Color.");
        var elevation = composer.Consume(SurfaceKt.LocalAbsoluteTonalElevation) as Dp
            ?? throw new InvalidOperationException("FAB content did not receive a native tonal elevation.");
        long packed = unchecked((long)color.Value);
        float dp = elevation.Value;
        new Text("+").Render(composer);
        composer.SideEffect(() =>
        {
            activity.ContentIdentity = identity;
            activity.ContentColor = packed;
            activity.TonalElevation = dp;
        });
    }
}
