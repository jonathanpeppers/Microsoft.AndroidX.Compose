using System.Linq;
using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.StateEffectsAnimation;

/// <summary>State factory methods on <see cref="Compose"/> mirroring Kotlin's <c>mutableStateOf</c> family.</summary>
public static class StateFactoriesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "state-factories",
        CategoryId:  "state-effects",
        Title:       "State factories",
        Description: "Compose.MutableStateOf, MutableIntStateOf, MutableLongStateOf, MutableFloatStateOf, MutableDoubleStateOf, MutableStateListOf, MutableStateMapOf — the Kotlin top-level factory family lifted onto Compose.cs.",
        Build:       () =>
        {
            var name   = Compose.Remember(() => Compose.MutableStateOf("Ada"));
            var i      = Compose.Remember(() => Compose.MutableIntStateOf(0));
            var l      = Compose.Remember(() => Compose.MutableLongStateOf(1_000_000_000L));
            var f      = Compose.Remember(() => Compose.MutableFloatStateOf(0.5f));
            var d      = Compose.Remember(() => Compose.MutableDoubleStateOf(System.Math.PI));
            var items  = Compose.Remember(() => Compose.MutableStateListOf("alpha", "beta"));
            var prefs  = Compose.Remember(() => Compose.MutableStateMapOf<string, bool>());

            return new Column
            {
                new Text($"MutableStateOf<string>: {name}"),
                new Button(onClick: () => name.Value = name.Value == "Ada" ? "Grace" : "Ada")
                    { new Text("Toggle name") },

                new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },

                new Text($"int: {i}    long: {l}    float: {f:F2}    double: {d:F4}"),
                new Row
                {
                    new Button(onClick: () => i++) { new Text("i++") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => l++) { new Text("l++") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => f.Value += 0.1f) { new Text("f += .1") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => d.Value /= 2) { new Text("d /= 2") },
                },

                new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },

                new Text($"MutableStateListOf ({items.Count}): [{string.Join(", ", items)}]"),
                new Row
                {
                    new Button(onClick: () => items.Add($"item{items.Count}")) { new Text("Add") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => { if (items.Count > 0) items.RemoveAt(items.Count - 1); })
                        { new Text("Remove") },
                },

                new HorizontalDivider { Modifier = Modifier.Companion.Padding(0, 8) },

                new Text($"MutableStateMapOf: {{{string.Join(", ", prefs.Select(kv => $"{kv.Key}={kv.Value}"))}}}"),
                new Row
                {
                    new Button(onClick: () => prefs["dark"] = !(prefs.TryGetValue("dark", out var v) && v))
                        { new Text("Toggle dark") },
                    new Spacer { Modifier = Modifier.Companion.Padding(4) },
                    new Button(onClick: () => prefs["compact"] = !(prefs.TryGetValue("compact", out var v) && v))
                        { new Text("Toggle compact") },
                },
            };
        });
}
