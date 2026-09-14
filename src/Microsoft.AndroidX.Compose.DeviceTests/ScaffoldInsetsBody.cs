using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class ScaffoldInsetsBody(ScaffoldInsetsFrame frame) : ComposableNode
{
    internal override void Render(IComposer composer, IntPtr contentPadding)
    {
        if (contentPadding == IntPtr.Zero)
            throw new InvalidOperationException("Scaffold did not forward body PaddingValues.");
        frame.RecordPadding(PaddingValues.Wrap(contentPadding));
        base.Render(composer, contentPadding);
    }

    /// <summary>Renders a measured child inside the ordinary node padding path.</summary>
    public override void Render(IComposer composer)
    {
        // Use the actual base-class padding path, then measure a distinct child inside it.
        var modifier = BuildModifier();
        var content = ComposableLambdas.Wrap3(composer, c =>
            new ScaffoldInsetsStateProbe(frame)
            {
                Modifier = Modifier.FillMaxSize(),
            }.Render(c));
        ComposeBridges.Box(
            modifier, content, propagateMinConstraints: false,
            defaults: (int)(BoxDefault.All & ~BoxDefault.Modifier),
            composer: composer);
    }
}
