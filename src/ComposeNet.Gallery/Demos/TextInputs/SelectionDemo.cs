using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.TextInputs;

/// <summary>
/// <see cref="SelectionContainer"/> + <see cref="DisableSelection"/> —
/// the only path to user-selectable / copyable Text in ComposeNet.
/// </summary>
public static class SelectionDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "text-selection",
        CategoryId:  "text-inputs",
        Title:       "Selectable text",
        Description: "SelectionContainer makes Text selectable; DisableSelection opts a subtree out.",
        Build:       () => new Column(verticalArrangement: Arrangement.SpacedBy(16))
        {
            new Text("1) Outside any SelectionContainer — long-press does nothing.")
            {
                FontWeight = ComposeNet.FontWeight.Medium,
            },
            new Text("Plain Text, not selectable."),

            new Text("2) Inside SelectionContainer — long-press to select, drag handles, copy.")
            {
                FontWeight = ComposeNet.FontWeight.Medium,
            },
            new SelectionContainer
            {
                new Text("Long-press anywhere in this paragraph to start a selection, then drag the handles to extend it and tap Copy in the system action bar."),
            },

            new Text("3) DisableSelection nested inside — the inner Text is skipped.")
            {
                FontWeight = ComposeNet.FontWeight.Medium,
            },
            new SelectionContainer
            {
                new Column
                {
                    new Text("Selectable line above the opt-out region."),
                    new DisableSelection
                    {
                        new Text("This middle line is NOT selectable — long-press skips over it."),
                    },
                    new Text("Selectable line below the opt-out region."),
                },
            },
        });
}
