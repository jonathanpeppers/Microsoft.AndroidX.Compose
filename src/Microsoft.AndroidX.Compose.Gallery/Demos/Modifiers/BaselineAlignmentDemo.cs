using AndroidX.Compose.Gallery.Registry;
using Baselines = AndroidX.Compose.UI.Layout.AlignmentLineKt;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>First and last baselines align different font sizes and multiline text.</summary>
public static class BaselineAlignmentDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "modifiers-baseline-alignment",
        CategoryId: "modifiers",
        Title: "First and last baselines",
        Description: "Different font sizes share a first baseline; multiline text shares a last baseline.",
        Build: _ => new Column
        {
            Modifier.Padding(16),
            new Text("First baseline"),
            new Row
            {
                new Text("Large") { FontSize = 32, Modifier = Modifier.AlignByBaseline() },
                new Text(" small") { FontSize = 16, Modifier = Modifier.AlignBy(Baselines.FirstBaseline) },
            },
            new Text("Last baseline") { Modifier = Modifier.Padding(top: 24) },
            new Row
            {
                new Text("Two\nlines") { FontSize = 24, Modifier = Modifier.AlignBy(Baselines.LastBaseline) },
                new Text(" last line") { FontSize = 16, Modifier = Modifier.AlignBy(Baselines.LastBaseline) },
            },
        });
}
