using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>MutableStateList + MutableStateMap — reactive collections.</summary>
public static class MutableStateCollectionsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-mutable-collections",
        CategoryId:  "state-effects",
        Title:       "MutableStateList + Map",
        Description: "Snapshot-tracked List and Map; mutate them and dependent reads recompose automatically.",
        Build:       () =>
        {
            var list = ComposeRuntime.Remember(() => new MutableStateList<string> { "alpha", "beta" });
            var map  = ComposeRuntime.Remember(() => new MutableStateMap<string, int> { ["alpha"] = 1, ["beta"] = 2 });

            return new Column
            {
                new Text($"List ({list.Count}): [{string.Join(", ", list)}]"),
                new Row
                {
                    new Button(onClick: () => list.Add($"item{list.Count}")) { new Text("Add") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => { if (list.Count > 0) list.RemoveAt(list.Count - 1); })
                        { new Text("Remove last") },
                },
                new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },
                new Text($"Map: {{{string.Join(", ", map.Select(kv => $"{kv.Key}={kv.Value}"))}}}"),
                new Row
                {
                    new Button(onClick: () =>
                    {
                        var key = $"k{map.Count}";
                        map[key] = map.Count + 1;
                    }) { new Text("Add") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => map.Clear()) { new Text("Clear") },
                },
            };
        });
}
