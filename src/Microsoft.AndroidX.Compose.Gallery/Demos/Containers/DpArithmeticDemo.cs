using AndroidX.Compose.Gallery.Registry;

namespace AndroidX.Compose.Gallery.Demos.Containers;

/// <summary>
/// <see cref="Dp"/> / <see cref="Sp"/> arithmetic operators and the
/// <c>int.Dp()</c> / <c>int.Sp()</c> extension methods, plus the typed
/// <see cref="Arrangement.SpacedBy(Dp)"/> overload.
/// </summary>
public static class DpArithmeticDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "containers-dp-arithmetic",
        CategoryId:  "containers",
        Title:       "Dp / Sp arithmetic",
        Description: "Typed Dp/Sp values flow through +, -, *, / and feed Modifier.Padding, Arrangement.SpacedBy without dropping to raw ints.",
        Build:       _ =>
        {
            // Hoist a base padding once, then derive the rest with typed
            // arithmetic — no .Value/.PackedValue gymnastics, no raw floats.
            Dp basePad   = 8.Dp();
            Dp doublePad = basePad * 2;       // 16.dp via Dp * float
            Dp triplePad = basePad + 16.Dp(); // 24.dp via Dp + Dp
            Dp gap       = basePad + 4.Dp();  // 12.dp

            // Sp arithmetic: scale a base size up for a heading.
            Sp bodySize    = 14.Sp();
            Sp headingSize = bodySize * 1.5f;

            return new Column(verticalArrangement: Arrangement.SpacedBy(gap))
            {
                Modifier.Companion.Padding(basePad),

                new Text($"basePad   = {basePad}")    { FontSize = bodySize },
                new Text($"doublePad = {doublePad}")  { FontSize = bodySize },
                new Text($"triplePad = {triplePad}")  { FontSize = bodySize },

                // Modifier slot consuming a Dp produced by arithmetic.
                new Box
                {
                    Modifier.Companion
                        .Size(80.Dp())
                        .Padding(doublePad)
                        .Background(Color.FromRgb(0xB3, 0xE5, 0xFC)),
                    new Text("Padding(basePad * 2)")
                    {
                        Color    = Color.Black,
                        FontSize = bodySize,
                    },
                },

                // Arrangement.SpacedBy now accepts a typed Dp directly.
                new Row(horizontalArrangement: Arrangement.SpacedBy(basePad))
                {
                    new Box
                    {
                        Modifier.Companion.Size(40.Dp()).Background(Color.FromRgb(0xFF, 0xCC, 0x80)),
                    },
                    new Box
                    {
                        Modifier.Companion.Size(40.Dp()).Background(Color.FromRgb(0xFF, 0xB7, 0x4D)),
                    },
                    new Box
                    {
                        Modifier.Companion.Size(40.Dp()).Background(Color.FromRgb(0xFF, 0xA7, 0x26)),
                    },
                },

                new Text($"heading = base * 1.5 = {headingSize.PackedValue:X16}")
                {
                    FontSize = headingSize,
                },
            };
        });
}
