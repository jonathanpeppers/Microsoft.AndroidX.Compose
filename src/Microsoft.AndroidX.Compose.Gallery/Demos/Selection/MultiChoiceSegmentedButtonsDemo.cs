using Microsoft.AndroidX.Compose.Gallery.Registry;

namespace Microsoft.AndroidX.Compose.Gallery.Demos.Selection;

/// <summary>Multi-choice SegmentedButtonRow — each button is independently checkable.</summary>
public static class MultiChoiceSegmentedButtonsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-segmented-multi",
        CategoryId:  "selection",
        Title:       "Multi-choice segmented buttons",
        Description: "MultiChoiceSegmentedButtonRow with two independently checkable SegmentedButtons.",
        Build:       c =>
        {
            var bold   = c.Remember(() => new MutableState<bool>(false));
            var italic = c.Remember(() => new MutableState<bool>(false));
            return new Column
            {
                new MultiChoiceSegmentedButtonRow
                {
                    new SegmentedButton(@checked: bold.Value,   onCheckedChange: v => bold.Value   = v) { new Text("Bold") },
                    new SegmentedButton(@checked: italic.Value, onCheckedChange: v => italic.Value = v) { new Text("Italic") },
                },
                new Text($"Bold: {bold.Value}, Italic: {italic.Value}"),
            };
        });
}
