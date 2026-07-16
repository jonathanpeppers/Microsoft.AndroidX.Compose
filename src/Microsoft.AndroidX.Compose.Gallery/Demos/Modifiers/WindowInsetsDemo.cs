using AndroidX.Compose.Gallery.Registry;
using static AndroidX.Compose.Composables;

namespace AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>
/// Exercises ambient platform inset readers, composition-aware padding
/// conversion, fixed inset construction, set operations, consumption,
/// and inset-sized spacers.
/// </summary>
public static class WindowInsetsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-window-insets",
        CategoryId:  "modifiers",
        Title:       "WindowInsets",
        Description: "Generic edge-to-edge padding, consumption, set operations, and IME sizing.",
        Build:       _ => new Composed(c =>
        {
            var text = c.MutableStateOf("");
            var safeDrawing = SafeDrawingInsets();
            var ime = ImeInsets();
            var safeDrawingWithoutIme = safeDrawing.Exclude(ime);
            var customInsets = c.Remember(
                () => new WindowInsets(left: 12, top: 8, right: 12, bottom: 8));
            var horizontalInsets = customInsets.Only(WindowInsetsSides.Horizontal);
            (string Name, WindowInsets Insets)[] platformInsets =
            [
                ("Caption bar", CaptionBarInsets()),
                ("Display cutout", DisplayCutoutInsets()),
                ("IME", ime),
                ("Mandatory system gestures", MandatorySystemGesturesInsets()),
                ("Navigation bars", NavigationBarsInsets()),
                ("Safe content", SafeContentInsets()),
                ("Safe drawing", safeDrawing),
                ("Safe gestures", SafeGesturesInsets()),
                ("Status bars", StatusBarsInsets()),
                ("System bars", SystemBarsInsets()),
                ("System gestures", SystemGesturesInsets()),
                ("Tappable element", TappableElementInsets()),
                ("Waterfall", WaterfallInsets()),
            ];

            var content = new Column
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

            foreach (var (name, insets) in platformInsets)
            {
                var padding = insets.AsPaddingValues();
                content.Add(new Text(
                    $"{name}: top {padding.Top.Value:F0}dp, bottom {padding.Bottom.Value:F0}dp"));
            }

            return content;
        }));
}
