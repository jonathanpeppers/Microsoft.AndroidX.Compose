using AndroidX.Compose.Gallery.Registry;
using Placeable = AndroidX.Compose.UI.Layout.Placeable;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>
/// Custom <see cref="Layout"/> primitive — adaptive multi-column layout
/// that bins variable-height cards into the fewest columns whose width
/// is at least <c>minColumnWidth</c>, distributing items to balance
/// the total height of each column.
/// </summary>
public static class CustomLayoutDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-custom-layout",
        CategoryId:  "containers",
        Title:       "Custom Layout (adaptive columns)",
        Description: "Uses Compose's low-level Layout primitive to bin cards into N columns " +
                     "based on available width, then distribute by total column height.",
        Build:       _ =>
        {
            // Sample articles with intentionally varied content lengths
            // so the height-balancing is visible.
            string[] titles =
            [
                "Compose ships 1.7",
                "Material 3 expressive",
                "Coroutines on JVM 21",
                "Wear OS 5 picker APIs",
                "Type-safe nav",
                "Kotlin K2 in stable Gradle",
                "Performance: SaveableStateHolder",
                "Behind the scenes of remember",
                "Designing for foldables",
                "Animations & Motion 2.0",
            ];
            string[] bodies =
            [
                "What's new in the latest stable.",
                "A practical look at the new colour system and dynamic theming on Android 16.",
                "Loom-friendly suspending APIs and how they coexist.",
                "Native picker support reduces glue code on small displays.",
                "How to migrate.",
                "Build-time wins and migration notes for K2.",
                "Save once, restore often: keying tips for state holders.",
                "A short tour through slot table mechanics.",
                "Adaptive layouts that breathe across hinge angles.",
                "Subcompose, transitions, and the road to 2.0.",
            ];

            var layout = new Layout(measurePolicy: (scope, measurables, constraints) =>
            {
                // Bin into N columns by available width. 220 dp is the
                // minimum desired column width.
                int min = scope.RoundToPx(220);
                int max = constraints.HasBoundedWidth
                    ? constraints.MaxWidth
                    : min;
                int columns = Math.Max(1, max / min);
                int columnWidth = max / columns;

                // Greedy shortest-column allocation: each measurable
                // joins the column whose running total height is
                // smallest, balancing the layout end-to-end.
                var childConstraints = Constraints.FixedWidth(columnWidth);
                var placeables = new Placeable[measurables.Count];
                var columnHeights = new int[columns];
                var columnAssign  = new int[measurables.Count];
                for (int i = 0; i < measurables.Count; i++)
                {
                    placeables[i] = measurables[i].Measure(childConstraints);
                    int target = 0;
                    for (int c = 1; c < columns; c++)
                        if (columnHeights[c] < columnHeights[target]) target = c;
                    columnAssign[i] = target;
                    columnHeights[target] += placeables[i].Height;
                }

                int totalHeight = 0;
                for (int c = 0; c < columns; c++)
                    if (columnHeights[c] > totalHeight) totalHeight = columnHeights[c];

                int layoutWidth = columnWidth * columns;

                return scope.Layout(layoutWidth, totalHeight, placement =>
                {
                    var yByColumn = new int[columns];
                    for (int i = 0; i < placeables.Length; i++)
                    {
                        int col = columnAssign[i];
                        placement.PlaceRelative(
                            placeables[i],
                            x: col * columnWidth,
                            y: yByColumn[col]);
                        yByColumn[col] += placeables[i].Height;
                    }
                });
            })
            {
                Modifier.Companion.FillMaxWidth(),
            };
            foreach (var card in BuildCards(titles, bodies))
                layout.Add(card);

            return new Column
            {
                Modifier.Companion.Padding(8),
                new Text("Resize the window — columns rebalance to fit available width."),
                new Spacer { Modifier = Modifier.Companion.Height(8) },
                layout,
            };
        });

    static IEnumerable<ComposableNode> BuildCards(
        string[] titles, string[] bodies)
    {
        for (int i = 0; i < titles.Length; i++)
        {
            string title = titles[i];
            string body  = bodies[i];
            yield return new Card
            {
                Modifier.Companion.Padding(4),
                new Column
                {
                    Modifier.Companion.Padding(12),
                    new Text(title),
                    new Spacer { Modifier = Modifier.Companion.Height(4) },
                    new Text(body),
                },
            };
        }
    }
}
