using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Selection;

/// <summary>RadioButton group with three options.</summary>
public static class RadioButtonGroup
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-radio-group",
        CategoryId:  "selection",
        Title:       "RadioButton group",
        Description: "Single-selection group built from three RadioButtons.",
        Build:       () =>
        {
            var picked = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new Row
                {
                    new RadioButton(selected: picked.Value == 0, onClick: () => picked.Value = 0),
                    new Text("A"),
                    new RadioButton(selected: picked.Value == 1, onClick: () => picked.Value = 1),
                    new Text("B"),
                    new RadioButton(selected: picked.Value == 2, onClick: () => picked.Value = 2),
                    new Text("C"),
                },
                new Text($"Picked: {(char)('A' + picked.Value)}"),
            };
        });
}
