using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.UI.Platform;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Compares logical versus physical corners using Dp and percent, uniform and per-corner overloads.</summary>
public static class AbsoluteCornerShapesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "containers-absolute-corner-shapes",
        CategoryId: "containers",
        Title: "Relative and absolute corners",
        Description: "CutCornerShape mirrors start/end in RTL; AbsoluteCutCornerShape and AbsoluteRoundedCornerShape keep physical corners.",
        Build: c =>
        {
            var kind = c.Remember(() => new MutableState<int>(0));
            return new Column(verticalArrangement: Arrangement.SpacedBy(12))
            {
                new Button(() => kind.Value = (kind.Value + 1) % 3) { new Text("Next shape family") },
                new Composed(_ => new Text(kind.Value switch
                {
                    0 => "CutCornerShape (relative start/end)",
                    1 => "AbsoluteCutCornerShape (physical left/right)",
                    _ => "AbsoluteRoundedCornerShape (physical left/right)",
                })),
                new Text("Asymmetric order: 30, 0, 12, 0. Only the relative family mirrors between previews."),
                Preview(false, kind),
                Preview(true, kind),
                new Text("Shape.CutCorners / CutCornersPercent per-corner factories:"),
                new Row(horizontalArrangement: Arrangement.SpacedBy(12))
                {
                    Tile("Dp", Shape.CutCorners(30.Dp(), 0.Dp(), 12.Dp(), 0.Dp())),
                    Tile("%", Shape.CutCornersPercent(30, 0, 12, 0)),
                },
            };
        });

    static ComposableNode Preview(bool rtl, MutableState<int> kind) => new Column
    {
        new Text(rtl ? "RTL" : "LTR"),
        new AndroidView(context =>
        {
            var view = new ComposeView(context)
            {
                LayoutDirection = rtl ? global::Android.Views.LayoutDirection.Rtl : global::Android.Views.LayoutDirection.Ltr,
            };
            view.SetContent(c => new Composed(composer =>
            {
                int family = kind.Value;
                var shapes = composer.Remember(() => Enumerable.Range(0, 4)
                    .Select(variant => Create(family, variant)).ToArray(), family);
                return new Column(verticalArrangement: Arrangement.SpacedBy(8))
                {
                    Modifier.FillMaxSize().Background(Color.White),
                    new Row(horizontalArrangement: Arrangement.SpacedBy(12))
                    {
                        Tile("Uniform 12 Dp", shapes[0]), Tile("Uniform 25%", shapes[1]),
                    },
                    new Row(horizontalArrangement: Arrangement.SpacedBy(12))
                    {
                        Tile("Per-corner Dp", shapes[2]), Tile("Per-corner %", shapes[3]),
                    },
                };
            }));
            return view;
        }, view => view.LayoutDirection = rtl
            ? global::Android.Views.LayoutDirection.Rtl : global::Android.Views.LayoutDirection.Ltr)
        { Modifier = Modifier.FillMaxWidth().Height(190) },
    };

    static Column Tile(string caption, Shape shape) => new()
    {
        Modifier.Background(Color.White),
        new Text(caption) { Color = Color.Black },
        new Box { Modifier.Size(120, 60).Clip(shape).Background(Color.Blue) },
    };

    static Shape Create(int family, int variant) => (family, variant) switch
    {
        (0, 0) => new CutCornerShape(12.Dp()),
        (0, 1) => new CutCornerShape(25),
        (0, 2) => new CutCornerShape(30.Dp(), 0.Dp(), 12.Dp(), 0.Dp()),
        (0, 3) => new CutCornerShape(30, 0, 12, 0),
        (1, 0) => new AbsoluteCutCornerShape(12.Dp()),
        (1, 1) => new AbsoluteCutCornerShape(25),
        (1, 2) => new AbsoluteCutCornerShape(30.Dp(), 0.Dp(), 12.Dp(), 0.Dp()),
        (1, 3) => new AbsoluteCutCornerShape(30, 0, 12, 0),
        (2, 0) => new AbsoluteRoundedCornerShape(12.Dp()),
        (2, 1) => new AbsoluteRoundedCornerShape(25),
        (2, 2) => new AbsoluteRoundedCornerShape(30.Dp(), 0.Dp(), 12.Dp(), 0.Dp()),
        (2, 3) => new AbsoluteRoundedCornerShape(30, 0, 12, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(family)),
    };
}
