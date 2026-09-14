using AndroidX.Compose.Gallery.Registry;
using Baselines = AndroidX.Compose.UI.Layout.AlignmentLineKt;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>Contrasts ordinary edge padding with baseline-relative distances.</summary>
public static class BaselinePaddingDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "modifiers-baseline-padding",
        CategoryId: "modifiers",
        Title: "Baseline-relative padding",
        Description: "PaddingFrom first/last baseline, and PaddingFromBaseline for both edges. Unspecified edges stay natural.",
        Build: _ => new Column
        {
            Modifier.Padding(16),
            new Text("Natural edges (both omitted)"),
            new Text("Hello") { Modifier = Tile().PaddingFromBaseline(), Color = Color.Black },
            new Text("Top to first baseline: 48 dp"),
            new Text("Hello") { Modifier = Tile().PaddingFrom(Baselines.FirstBaseline, before: 48), Color = Color.Black },
            new Text("Last baseline to bottom: 32 dp"),
            new Text("Two\nlines") { Modifier = Tile().PaddingFrom(Baselines.LastBaseline, after: 32), Color = Color.Black },
            new Text("First/top: 48 dp; last/bottom: 32 dp"),
            new Text("Two\nlines") { Modifier = Tile().PaddingFromBaseline(top: 48, bottom: 32), Color = Color.Black },
        });

    static Modifier Tile() => Modifier.FillMaxWidth().Background(Color.LightGray);
}
