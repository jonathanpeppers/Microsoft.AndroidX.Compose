using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>Crossfade&lt;T&gt; — cross-dissolve between content keyed on a value.</summary>
public static class CrossfadeDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "anim-crossfade",
        CategoryId:  "state",
        Title:       "Crossfade<int>",
        Description: "Whenever the int changes, the previous card fades out as the new one fades in.",
        Build:       () =>
        {
            var step = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Row(horizontalArrangement: Arrangement.SpacedBy(8))
                {
                    new Button(onClick: () => step--) { new Text("−") },
                    new Button(onClick: () => step++) { new Text("+") },
                },
                new Crossfade<int>(
                    targetState: step.Value,
                    content:     i => new Card
                    {
                        Modifier.Companion.Padding(8),
                        new Text($"Step {i}"),
                    }),
            };
        });
}
