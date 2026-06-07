using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.TextInputs;

/// <summary>MaxLines / MinLines / Overflow / SoftWrap.</summary>
public static class OverflowAndClamping
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-overflow-clamping",
        CategoryId:  "text-inputs",
        Title:       "Overflow & line clamping",
        Description: "Cap lines, force ellipsis, disable soft wrap.",
        Build:       () => new Column
        {
            new Text("Single line, ellipsised when wide — like this one which keeps going and going and going off the edge of the screen.")
            {
                MaxLines = 1,
                Overflow = ComposeNet.TextOverflow.Ellipsis,
                SoftWrap = false,
            },
            new Text("Two-line cap with MinLines = 2 — short text still reserves the second line."),
            new Text("Two-line cap with MinLines = 2 — long text is ellipsised at the cap, and short content above still reserves vertical space.")
            {
                MaxLines = 2,
                MinLines = 2,
                Overflow = ComposeNet.TextOverflow.Ellipsis,
            },
        });
}
