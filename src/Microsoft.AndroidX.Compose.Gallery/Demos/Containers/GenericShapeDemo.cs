using AndroidX.Compose.Gallery.Registry;
using AndroidX.Compose.UI.Platform;
using LayoutDirection = AndroidX.Compose.UI.Unit.LayoutDirection;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>Builds a directional triangle from the native borrowed path and pixel bounds.</summary>
public static class GenericShapeDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id: "containers-generic-shape",
        CategoryId: "containers",
        Title: "GenericShape",
        Description: "A remembered pixel-path callback. LTR points right; RTL points left. Resize after managed/Java GC.",
        Build: c =>
        {
            var wide = c.Remember(() => new MutableState<bool>(false));
            return new Column(verticalArrangement: Arrangement.SpacedBy(12))
            {
                new Text("The builder receives a fresh borrowed Path, pixel Size and native LayoutDirection."),
                new Button(() =>
                {
                    GC.Collect();
                    Java.Lang.JavaSystem.Gc();
                    wide.Value = !wide.Value;
                }) { new Text("GC and resize") },
                Preview(false, wide),
                Preview(true, wide),
            };
        });

    static ComposableNode Preview(bool rtl, MutableState<bool> wide) => new Column
    {
        new Text(rtl ? "RTL: point faces left" : "LTR: point faces right"),
        new AndroidView(context =>
        {
            var view = new ComposeView(context)
            {
                LayoutDirection = rtl ? global::Android.Views.LayoutDirection.Rtl : global::Android.Views.LayoutDirection.Ltr,
            };
            view.SetContent(c => new Composed(composer =>
            {
                var shape = composer.Remember(() => new GenericShape((path, size, direction) =>
                {
                    bool reversed = direction == LayoutDirection.Rtl;
                    path.MoveTo(reversed ? size.Width : 0, 0)
                        .LineTo(reversed ? 0 : size.Width, size.Height / 2)
                        .LineTo(reversed ? size.Width : 0, size.Height);
                }));
                return new Box
                {
                    Modifier.FillMaxSize().Background(Color.White),
                    new Box { Modifier.Size(wide.Value ? 200 : 120, 80).Clip(shape).Background(Color.Blue) },
                };
            }));
            return view;
        }, view => view.LayoutDirection = rtl
            ? global::Android.Views.LayoutDirection.Rtl : global::Android.Views.LayoutDirection.Ltr)
        { Modifier = Modifier.FillMaxWidth().Height(90) },
    };
}
