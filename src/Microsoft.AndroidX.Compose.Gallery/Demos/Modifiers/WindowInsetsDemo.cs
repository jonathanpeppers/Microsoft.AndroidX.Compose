using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>
/// Exercises live system/IME insets, fixed inset construction, set
/// operations, generic padding, consumption, and inset-sized spacers.
/// </summary>
public static class WindowInsetsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-window-insets",
        CategoryId:  "modifiers",
        Title:       "WindowInsets",
        Description: "Generic edge-to-edge padding, consumption, set operations, and IME sizing.",
        Build:       c =>
        {
            var text = c.MutableStateOf("");
            var safeDrawing = WindowInsets.SafeDrawing(c);
            var ime = WindowInsets.Ime(c);
            var safeDrawingWithoutIme = safeDrawing.Exclude(ime);
            var customInsets = c.Remember(
                () => new WindowInsets(left: 12, top: 8, right: 12, bottom: 8));
            var horizontalInsets = customInsets.Only(WindowInsetsSides.Horizontal);

            return new Column
            {
                Modifier.WindowInsetsPadding(safeDrawingWithoutIme),
                new Text("SafeDrawing keeps this content clear of system UI."),
                new TextField(text, singleLine: true)
                {
                    Label = new Text("Focus to show the IME"),
                },
                new Box
                {
                    Modifier
                        .WindowInsetsPadding(horizontalInsets)
                        .ConsumeWindowInsets(horizontalInsets)
                        .Background(Color.FromHex("#FFF59D")),
                    new Text("12.dp fixed start/end insets")
                    {
                        Color = Color.Black,
                    },
                },
                new Text("The bar below follows the current IME bottom inset."),
                new Spacer
                {
                    Modifier = Modifier
                        .WindowInsetsBottomHeight(ime)
                        .FillMaxWidth()
                        .Background(Color.FromHex("#90CAF9")),
                },
            };
        });
}
