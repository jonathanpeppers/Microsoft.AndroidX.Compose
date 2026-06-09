using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>DerivedStateOf — a State&lt;T&gt; computed from other States.</summary>
public static class DerivedStateDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-derivedstate",
        CategoryId:  "state-effects",
        Title:       "DerivedStateOf",
        Description: "derived.Value reads list.Count inside DerivedStateOf, so anything that reads derived recomposes only when the count actually changes.",
        Build:       c =>
        {
            var list    = c.Remember(() => new MutableStateList<string> { "alpha", "beta" });
            var derived = c.Remember(() => ComposeExtensions.DerivedStateOf(() => list.Count));

            return new Column
            {
                new Text($"DerivedState (list.Count): {derived.Value}"),
                new Text($"List: [{string.Join(", ", list)}]"),
                new Row
                {
                    new Button(onClick: () => list.Add($"item{list.Count}")) { new Text("Add") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => { if (list.Count > 0) list.RemoveAt(list.Count - 1); })
                        { new Text("Remove last") },
                },
            };
        });
}
