using AndroidX.Compose.Gallery.Demos.ComposableMethods;
using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Constrained rows and columns with interactive, post-measure overflow counts.</summary>
public static class FlowOverflowDemo
{
    /// <summary>Tree-style flow overflow demo.</summary>
    public static Demo Demo => new(
        Id: "containers-flow-overflow",
        CategoryId: "containers",
        Title: "Flow overflow (tree)",
        Description: "Expand/collapse rows and columns; counts are drawn after measurement.",
        Build: _ => new ComposableDemoAdapter(() => Content(false)));

    /// <summary>Composerless flow overflow demo.</summary>
    public static Demo DirectDemo => new(
        Id: "containers-flow-overflow-direct",
        CategoryId: "containers",
        Title: "Flow overflow (composable)",
        Description: "Composerless callbacks with the same constrained, interactive flows.",
        Build: _ => new ComposableDemoAdapter(() => Content(true)));

    [Composable]
    internal static void Content(bool direct)
    {
        var rows = Composables.Remember(() => new MutableState<int>(1));
        var columns = Composables.Remember(() => new MutableState<int>(1));
        var count = Composables.Remember(() => new MutableState<int>(8));
        Composables.Column(() =>
        {
            Composables.Text("Native scope snapshots, read during drawing. Item-only changes may retain old counts.");
            Composables.Button(() => count.Value = count.Value == 8 ? 5 : 8,
                () => Composables.Text($"Toggle items (now {count.Value})"));
            Composables.Text("Row: three cells per row");
            if (direct)
            {
                Composables.FlowRow(() => Cells(count.Value), 3, rows.Value, Modifier.Width(240),
                    FlowRowOverflow.ExpandOrCollapseIndicator(
                        scope => Composables.Box(() => Composables.Text("More", color: Color.White),
                            modifier: Indicator(scope, () => rows.Value = 4, true)),
                        scope => Composables.Box(() => Composables.Text("Less", color: Color.White),
                            modifier: Indicator(scope, () => rows.Value = 1, false))));
            }
            else
            {
                var flow = new FlowRow(3, rows.Value)
                {
                    Overflow = FlowRowOverflow.ExpandOrCollapseIndicator(
                        scope => IndicatorNode(scope, () => rows.Value = 4, true),
                        scope => IndicatorNode(scope, () => rows.Value = 1, false)),
                };
                flow.Add(Modifier.Width(240));
                for (int i = 0; i < count.Value; i++) flow.Add(Cell(i));
                flow.Render(ComposableContext.Current);
            }
            Composables.Text("Column: three cells per column");
            if (direct)
            {
                Composables.FlowColumn(() => Cells(count.Value), 3, columns.Value,
                    Modifier.Height(144).Width(240),
                    FlowColumnOverflow.ExpandOrCollapseIndicator(
                        scope => Composables.Box(() => Composables.Text("More", color: Color.White),
                            modifier: Indicator(scope, () => columns.Value = 4, true)),
                        scope => Composables.Box(() => Composables.Text("Less", color: Color.White),
                            modifier: Indicator(scope, () => columns.Value = 1, false))));
            }
            else
            {
                var flow = new FlowColumn(3, columns.Value)
                {
                    Overflow = FlowColumnOverflow.ExpandOrCollapseIndicator(
                        scope => IndicatorNode(scope, () => columns.Value = 4, true),
                        scope => IndicatorNode(scope, () => columns.Value = 1, false)),
                };
                flow.Add(Modifier.Height(144).Width(240));
                for (int i = 0; i < count.Value; i++) flow.Add(Cell(i));
                flow.Render(ComposableContext.Current);
            }
            Composables.Text("Each collapsed flow shows 2 regular cells plus its indicator.");
        });
    }

    [Composable]
    internal static void Cells(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int index = i;
            Composables.Box(() => Composables.Text($"Item {index + 1}", color: Color.Black),
                modifier: Modifier.Width(80).Height(48).Background(Color.LightGray));
        }
    }

    static ComposableNode Cell(int index) => new Box
    {
        Modifier.Width(80).Height(48).Background(Color.LightGray),
        new Text($"Item {index + 1}") { Color = Color.Black },
    };

    static ComposableNode IndicatorNode(FlowOverflowScope scope, Action click, bool expand) => new Box
    {
        Indicator(scope, click, expand),
        new Text(expand ? "More" : "Less") { Color = Color.White },
    };

    static Modifier Indicator(FlowOverflowScope scope, Action click, bool expand) =>
        Modifier.Width(80).Height(48).Background(Color.Blue)
            .Semantics(expand ? "Expand flow" : "Collapse flow")
            .Clickable(click).DrawWithContent(draw =>
            {
                draw.DrawContent();
                using var paint = new global::Android.Graphics.Paint
                {
                    AntiAlias = true,
                    Color = global::Android.Graphics.Color.White,
                    TextSize = draw.Size.Height * 0.3f,
                };
                draw.NativeCanvas.DrawText($"{scope.ShownItemCount}/{scope.TotalItemCount}",
                    4, draw.Size.Height * 0.9f, paint);
            });
}
