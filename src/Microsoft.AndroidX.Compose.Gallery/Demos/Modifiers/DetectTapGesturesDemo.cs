using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Modifiers;

/// <summary>DetectTapGestures — onTap / onPress / onLongPress / onDoubleTap with hit-position offsets.</summary>
public static class DetectTapGesturesDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "modifiers-detect-tap-gestures",
        CategoryId:  "modifiers",
        Title:       "DetectTapGestures",
        Description: "Low-level pointer-input modifier: tap, press, long-press, double-tap — each reports the hit Offset.",
        Build:       c =>
        {
            var lastEvent = c.Remember(() => new MutableState<string>("(none)"));
            var taps = c.Remember(() => new MutableNumberState<int>(0));

            string Fmt(string label, Offset offset) =>
                $"{label} at ({offset.X:F0}, {offset.Y:F0})";

            return new Column
            {
                Modifier.Companion.FillMaxWidth().Padding(8),
                new Text("Tap, hold, or double-tap the surface below"),
                new Text($"Taps: {taps}    Last: {lastEvent.Value}"),
                new Box
                {
                    Modifier.Companion
                        .FillMaxWidth()
                        .Height(160)
                        .Background(Color.FromArgb(0xFFC8E6C9))
                        .DetectTapGestures(
                            onTap:        o => { taps.Value += 1;  lastEvent.Value = Fmt("Tap",        o); },
                            onPress:      o => {                    lastEvent.Value = Fmt("Press",      o); },
                            onLongPress:  o => {                    lastEvent.Value = Fmt("LongPress",  o); },
                            onDoubleTap:  o => { taps.Value += 10; lastEvent.Value = Fmt("DoubleTap",  o); })
                        .Padding(16),
                    new Text("Surface (tap me)") { Color = Color.Black },
                },
                new Button(onClick: () => { taps.Value = 0; lastEvent.Value = "(none)"; })
                {
                    new Text("Reset"),
                },
            };
        });
}
