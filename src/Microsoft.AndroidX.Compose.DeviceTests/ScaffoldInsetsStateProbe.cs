using AndroidX.Compose;
using AndroidX.Compose.Runtime;

namespace Microsoft.AndroidX.Compose.DeviceTests;

internal sealed class ScaffoldInsetsStateProbe(ScaffoldInsetsFrame frame) : ComposableNode
{
    /// <summary>Owns remembered and saveable state inside Scaffold's measured content subtree.</summary>
    public override void Render(IComposer composer)
    {
        var sentinel = composer.Remember(static () => new object());
        var counter = composer.RememberSaveable(() => new MutableNumberState<int>(0));
        var content = ComposableLambdas.Wrap3(composer, c =>
        {
            frame.RecordBodyState(sentinel, counter);
            new Box
            {
                Modifier.FillMaxSize().Then(frame.Measure(ScaffoldInsetsFrame.Body)),
                new Box
                {
                    Modifier = Modifier.Width(frame.LayoutToken).Height(1)
                        .Then(frame.Measure(ScaffoldInsetsFrame.LayoutAck)),
                },
                new Text($"Saved counter: {counter.Value}"),
            }.Render(c);
        });
        ComposeBridges.Box(
            BuildModifier(), content, propagateMinConstraints: false,
            defaults: (int)(BoxDefault.All & ~BoxDefault.Modifier),
            composer: composer);
    }
}
