using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>
/// The README's "side-by-side Kotlin vs C#" hello-world sample, rendered
/// inside the gallery. A <see cref="Text"/>, a <see cref="Button"/>, and
/// a counter created via <see cref="ComposeExtensions.MutableStateOf(AndroidX.Compose.Runtime.IComposer, int, int, string)"/>.
/// </summary>
/// <remarks>
/// Doubles as the runtime exerciser for the implicit
/// <c>MutableState&lt;T&gt; → T</c> conversion: <c>count &gt; 0</c> reads the
/// wrapper as an <c>int</c> without an explicit <c>.Value</c>.
/// </remarks>
public static class HelloCounterDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-hello",
        CategoryId:  "buttons",
        Title:       "Hello from .NET",
        Description: "Material 3 hello-world from the README: Text + Button + counter via c.MutableStateOf(0).",
        Build:       c =>
        {
            var count = c.MutableStateOf(0);
            return new Column
            {
                new Text("Hello from .NET"),
                new Text($"Count: {count}"),
                new Button(onClick: () => count++)
                {
                    new Text("Tap to increment"),
                },
                count > 0
                    ? new Text("count is non-zero")
                    : null,
            };
        });
}
