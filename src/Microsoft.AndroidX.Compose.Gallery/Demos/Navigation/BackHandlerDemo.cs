using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Navigation;

/// <summary>
/// BackHandler demo — toggle interception of the system back press
/// and watch a counter tick each time the back gesture is consumed.
/// </summary>
public static class BackHandlerDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "navigation-back-handler",
        CategoryId:  "navigation",
        Title:       "BackHandler — intercept system back",
        Description: "Toggle interception of the back gesture; the counter ticks each time it's intercepted.",
        Build:       c =>
        {
            var intercept = c.Remember(() => new MutableState<bool>(true));
            var pressCount = c.Remember(() => new MutableNumberState<int>(0));

            return new Column
            {
                Modifier.Companion.Padding(16),

                new BackHandler(
                    onBack:  () => pressCount++,
                    enabled: intercept.Value),

                new Text("BackHandler is registered while this screen is active."),
                Spacer.Height(12),

                new Row
                {
                    new Switch(@checked: intercept.Value, onCheckedChange: v => intercept.Value = v),
                    Spacer.Width(12),
                    new Text(intercept.Value
                        ? "Intercepting — system back is consumed"
                        : "Letting back through — pops the gallery screen"),
                },
                Spacer.Height(16),

                new Text($"Back presses intercepted: {pressCount}"),
                Spacer.Height(16),

                new Text("Try pressing the system back button or swiping from the edge:"),
                new Text("• With the switch ON the counter ticks and you stay on this screen."),
                new Text("• With the switch OFF the gesture pops back to the gallery list."),
            };
        });
}
