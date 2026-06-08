using ComposeNet.Gallery.Registry;

namespace ComposeNet.Gallery.Demos.Selection;

/// <summary>Single-choice SegmentedButtonRow with three options.</summary>
public static class SingleChoiceSegmentedButtonsDemo
{
    /// <summary>Registry entry exposed via <see cref="Catalog.Demos"/>.</summary>
    public static Demo Demo => new(
        Id:          "selection-segmented-single",
        CategoryId:  "selection",
        Title:       "Single-choice segmented buttons",
        Description: "SingleChoiceSegmentedButtonRow with three SegmentedButtons.",
        Build:       () =>
        {
            var picked = Compose.Remember(() => new MutableNumberState<int>(0));
            return new Column
            {
                new SingleChoiceSegmentedButtonRow
                {
                    new SegmentedButton(selected: picked.Value == 0, onClick: () => picked.Value = 0) { new Text("Day") },
                    new SegmentedButton(selected: picked.Value == 1, onClick: () => picked.Value = 1) { new Text("Week") },
                    new SegmentedButton(selected: picked.Value == 2, onClick: () => picked.Value = 2) { new Text("Month") },
                },
                new Text($"Selected: {picked}"),
            };
        });
}
