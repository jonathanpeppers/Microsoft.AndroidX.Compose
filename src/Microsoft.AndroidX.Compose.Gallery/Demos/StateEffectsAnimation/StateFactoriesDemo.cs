using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.StateEffectsAnimation;

/// <summary>State factory ctors mirroring Kotlin's <c>mutableStateOf</c> family.</summary>
public static class StateFactoriesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-factories",
        CategoryId:  "state-effects",
        Title:       "State factories",
        Description: "MutableState<T>, MutableNumberState<T> (int/long/float/double), MutableStateList<T>, MutableStateMap<K,V> — the Kotlin top-level factory family expressed as plain C# ctors.",
        Build:       c =>
        {
            var name   = c.MutableStateOf("Ada");
            var i      = c.MutableStateOf(0);
            var l      = c.MutableStateOf(1_000_000_000L);
            var f      = c.MutableStateOf(0.5f);
            var d      = c.MutableStateOf(Math.PI);
            var items  = c.Remember(() => new MutableStateList<string>("alpha", "beta"));
            var prefs  = c.Remember(() => new MutableStateMap<string, bool>());

            return new Column
            {
                new Text($"MutableStateOf<string>: {name}"),
                new Button(onClick: () => name.Value = name.Value == "Ada" ? "Grace" : "Ada")
                    { new Text("Toggle name") },

                new HorizontalDivider { Modifier = Modifier.Padding(0, 8) },

                new Text($"int: {i}    long: {l}    float: {f:F2}    double: {d:F4}"),
                new Row
                {
                    new Button(onClick: () => i++) { new Text("i++") },
                    new Spacer { Modifier = Modifier.Padding(4) },
                    new Button(onClick: () => l++) { new Text("l++") },
                    new Spacer { Modifier = Modifier.Padding(4) },
                    new Button(onClick: () => f.Value += 0.1f) { new Text("f += .1") },
                    new Spacer { Modifier = Modifier.Padding(4) },
                    new Button(onClick: () => d.Value /= 2) { new Text("d /= 2") },
                },

                new HorizontalDivider { Modifier = Modifier.Padding(0, 8) },

                new Text($"MutableStateListOf ({items.Count}): [{string.Join(", ", items)}]"),
                new Row
                {
                    new Button(onClick: () => items.Add($"item{items.Count}")) { new Text("Add") },
                    new Spacer { Modifier = Modifier.Padding(4) },
                    new Button(onClick: () => { if (items.Count > 0) items.RemoveAt(items.Count - 1); })
                        { new Text("Remove") },
                },

                new HorizontalDivider { Modifier = Modifier.Padding(0, 8) },

                new Text($"MutableStateMapOf: {{{string.Join(", ", prefs.Select(kv => $"{kv.Key}={kv.Value}"))}}}"),
                new Row
                {
                    new Button(onClick: () => prefs["dark"] = !(prefs.TryGetValue("dark", out var v) && v))
                        { new Text("Toggle dark") },
                    new Spacer { Modifier = Modifier.Padding(4) },
                    new Button(onClick: () => prefs["compact"] = !(prefs.TryGetValue("compact", out var v) && v))
                        { new Text("Toggle compact") },
                },
            };
        });
}
