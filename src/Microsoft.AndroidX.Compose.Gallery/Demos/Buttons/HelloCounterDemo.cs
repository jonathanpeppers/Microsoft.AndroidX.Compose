using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Buttons;

/// <summary>
/// The README's "side-by-side Kotlin vs C#" hello-world sample, rendered
/// inside the gallery. A <see cref="Text"/>, a <see cref="Button"/>, and
/// a counter backed by <see cref="MutableNumberState{T}"/>.
/// </summary>
public static class HelloCounterDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "buttons-hello",
        CategoryId:  "buttons",
        Title:       "Hello from .NET",
        Description: "Material 3 hello-world from the README: Text + Button + counter via MutableNumberState<int>.",
        Build:       c =>
        {
            var count = c.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Text("Hello from .NET"),
                new Text($"Count: {count}"),
                new Button(onClick: () => count++)
                {
                    new Text("Tap to increment"),
                },
            };
        });
}
